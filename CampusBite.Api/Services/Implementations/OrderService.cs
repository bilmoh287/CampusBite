using CampusBite.Api.Domain.Entities;
using CampusBite.Api.Infrastructure.Data;
using CampusBite.Api.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Enums;
using CampusBite.Shared.Requests;
using CampusBite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace CampusBite.Api.Services.Implementations;

public class OrderService : IOrderService
{
    private readonly CampusBiteDbContext _context;
    private readonly ILogger<OrderService> _logger;

    public OrderService(CampusBiteDbContext context, ILogger<OrderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<OrderDto>> CreateOrderAsync(int customerId, CreateOrderRequest request)
    {
        // 1. Validate Café
        var cafe = await _context.Cafes.FindAsync(request.CafeId);
        if (cafe == null || !cafe.IsActive)
        {
            return ApiResponse<OrderDto>.FailureResult("The selected café is currently unavailable.");
        }

        // 2. Fetch products and check stock availability
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id) && p.CafeId == request.CafeId)
            .ToDictionaryAsync(p => p.Id);

        if (products.Count != productIds.Count)
        {
            return ApiResponse<OrderDto>.FailureResult("One or more items in your order do not belong to this café or are invalid.");
        }

        decimal subtotal = 0m;
        var orderItems = new List<OrderItem>();

        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];

            if (!product.IsAvailable)
            {
                return ApiResponse<OrderDto>.FailureResult($"Item '{product.Name}' is currently marked unavailable.");
            }

            // Inventory check
            if (product.StockQuantity.HasValue)
            {
                if (product.StockQuantity.Value < item.Quantity)
                {
                    return ApiResponse<OrderDto>.FailureResult($"Insufficient stock for '{product.Name}'. Available: {product.StockQuantity.Value}, Requested: {item.Quantity}.");
                }

                // Deduct stock
                product.StockQuantity -= item.Quantity;
            }

            var itemTotal = product.Price * item.Quantity;
            subtotal += itemTotal;

            // Snapshot pattern: Freeze product name and price at moment of purchase
            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = item.Quantity,
                ItemTotal = itemTotal
            });
        }

        // 3. Minimum order check
        if (cafe.MinimumOrderAmount > 0 && subtotal < cafe.MinimumOrderAmount)
        {
            return ApiResponse<OrderDto>.FailureResult($"Order subtotal ({subtotal:C}) is below the café minimum order of {cafe.MinimumOrderAmount:C}.");
        }

        decimal deliveryFee = cafe.DeliveryFee;
        decimal discountAmount = 0m;
        decimal totalAmount = subtotal + deliveryFee;

        // 4. Meal Contract Handling (if payment method is MealContract)
        StudentContract? activeContract = null;
        if (request.PaymentMethod == PaymentMethod.MealContract)
        {
            activeContract = await _context.StudentContracts
                .Include(sc => sc.ContractPlan)
                .Where(sc => sc.StudentId == customerId && 
                             sc.ContractPlan.CafeId == request.CafeId && 
                             sc.Status == ContractStatus.Active &&
                             sc.EndDate >= DateTime.UtcNow)
                .OrderBy(sc => sc.EndDate)
                .FirstOrDefaultAsync();

            if (activeContract == null)
            {
                return ApiResponse<OrderDto>.FailureResult("You do not have an active meal subscription plan with this café.");
            }

            // Check remaining credits and overdraft tab allowance
            int totalAvailableCredits = activeContract.RemainingCredits + (activeContract.AllowOverdraft ? activeContract.OverdraftLimit : 0);
            if (totalAvailableCredits < 1)
            {
                return ApiResponse<OrderDto>.FailureResult("Your meal contract credits and overdraft allowance are completely depleted.");
            }

            // Deduct 1 credit for the meal
            activeContract.RemainingCredits -= 1;
            discountAmount = subtotal; // Meal covered by contract!
            totalAmount = deliveryFee; // Student only pays delivery fee (or zero if pickup)

            if (activeContract.RemainingCredits <= 0 && !activeContract.AllowOverdraft)
            {
                activeContract.Status = ContractStatus.Depleted;
            }
        }

        // 5. Generate unique ticket number
        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerId = customerId,
            CafeId = request.CafeId,
            Status = OrderStatus.Pending,
            PaymentMethod = request.PaymentMethod,
            Subtotal = subtotal,
            DeliveryFee = deliveryFee,
            DiscountAmount = discountAmount,
            TotalAmount = totalAmount,
            DeliveryLocation = request.DeliveryLocation.Trim(),
            SpecialNotes = request.SpecialNotes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            OrderItems = orderItems
        };

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        // 6. Record Contract Usage Log (Immutable Audit Ledger)
        if (activeContract != null)
        {
            var usageLog = new ContractUsageLog
            {
                StudentContractId = activeContract.Id,
                OrderId = order.Id,
                CreditsDeducted = 1,
                RemainingCreditsAfter = activeContract.RemainingCredits,
                UsedAt = DateTime.UtcNow
            };
            await _context.ContractUsageLogs.AddAsync(usageLog);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Order {OrderNumber} created successfully by Customer {CustomerId} at Cafe {CafeId}", order.OrderNumber, customerId, request.CafeId);

        return await GetOrderByIdAsync(order.Id);
    }

    public async Task<ApiResponse<List<OrderDto>>> GetOrdersForCustomerAsync(int customerId)
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Cafe)
            .Include(o => o.Courier)
            .Include(o => o.OrderItems)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => MapToDto(o))
            .ToListAsync();

        return ApiResponse<List<OrderDto>>.SuccessResult(orders);
    }

    public async Task<ApiResponse<List<OrderDto>>> GetOrdersForCafeAsync(int cafeId)
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Courier)
            .Include(o => o.OrderItems)
            .Where(o => o.CafeId == cafeId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => MapToDto(o))
            .ToListAsync();

        return ApiResponse<List<OrderDto>>.SuccessResult(orders);
    }

    public async Task<ApiResponse<List<OrderDto>>> GetAvailableOrdersForCourierAsync()
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Cafe)
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .Where(o => o.Status == OrderStatus.ReadyForPickup && o.CourierId == null)
            .OrderBy(o => o.CreatedAt)
            .Select(o => MapToDto(o))
            .ToListAsync();

        return ApiResponse<List<OrderDto>>.SuccessResult(orders);
    }

    public async Task<ApiResponse<OrderDto>> GetOrderByIdAsync(int id)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Cafe)
            .Include(o => o.Customer)
            .Include(o => o.Courier)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return ApiResponse<OrderDto>.FailureResult("Order not found.");
        }

        return ApiResponse<OrderDto>.SuccessResult(MapToDto(order));
    }

    public async Task<Order?> GetOrderEntityByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Cafe)
            .Include(o => o.Customer)
            .Include(o => o.Courier)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<ApiResponse<OrderDto>> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, int currentUserId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return ApiResponse<OrderDto>.FailureResult("Order not found.");
        }

        // Validate state transitions
        if (order.Status == OrderStatus.Delivered)
        {
            return ApiResponse<OrderDto>.FailureResult("Order has already been delivered and cannot be altered.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return ApiResponse<OrderDto>.FailureResult("Order has already been cancelled.");
        }

        // If transitioning to Cancelled, restore stock!
        if (newStatus == OrderStatus.Cancelled)
        {
            var productIds = order.OrderItems.Select(oi => oi.ProductId).Where(p => p.HasValue).Select(p => p!.Value).ToList();
            var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

            foreach (var item in order.OrderItems)
            {
                if (item.ProductId.HasValue)
                {
                    var product = products.FirstOrDefault(p => p.Id == item.ProductId.Value);
                    if (product != null && product.StockQuantity.HasValue)
                    {
                        product.StockQuantity += item.Quantity; // Restore stock
                    }
                }
            }
        }

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Order {OrderNumber} status updated to {NewStatus} by User {UserId}", order.OrderNumber, newStatus, currentUserId);

        return await GetOrderByIdAsync(orderId);
    }

    public async Task<ApiResponse<OrderDto>> ClaimOrderAsync(int orderId, int courierId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            return ApiResponse<OrderDto>.FailureResult("Order not found.");
        }

        if (order.Status != OrderStatus.ReadyForPickup || order.CourierId != null)
        {
            return ApiResponse<OrderDto>.FailureResult("Order is not available for pickup or has already been claimed by another runner.");
        }

        order.CourierId = courierId;
        order.Status = OrderStatus.OutForDelivery;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Order {OrderNumber} claimed by Courier ID {CourierId}", order.OrderNumber, courierId);

        return await GetOrderByIdAsync(orderId);
    }

    private static OrderDto MapToDto(Order o)
    {
        return new OrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer?.FullName ?? string.Empty,
            CafeId = o.CafeId,
            CafeName = o.Cafe?.Name ?? string.Empty,
            CourierId = o.CourierId,
            CourierName = o.Courier?.FullName,
            Status = o.Status,
            PaymentMethod = o.PaymentMethod,
            Subtotal = o.Subtotal,
            DeliveryFee = o.DeliveryFee,
            DiscountAmount = o.DiscountAmount,
            TotalAmount = o.TotalAmount,
            DeliveryLocation = o.DeliveryLocation,
            SpecialNotes = o.SpecialNotes,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt,
            Items = o.OrderItems.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                ProductName = oi.ProductName,
                UnitPrice = oi.UnitPrice,
                Quantity = oi.Quantity,
                ItemTotal = oi.ItemTotal
            }).ToList()
        };
    }
}
