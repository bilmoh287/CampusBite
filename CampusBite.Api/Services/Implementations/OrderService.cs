using CampusBite.Api.Domain.Entities;
using CampusBite.Api.Hubs;
using CampusBite.Api.Infrastructure.Data;
using CampusBite.Api.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Enums;
using CampusBite.Shared.Requests;
using CampusBite.Shared.Responses;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CampusBite.Api.Services.Implementations;

public class OrderService : IOrderService
{
    private readonly CampusBiteDbContext _context;
    private readonly ILogger<OrderService> _logger;
    private readonly IHubContext<OrderHub> _hubContext;

    public OrderService(CampusBiteDbContext context, ILogger<OrderService> logger, IHubContext<OrderHub> hubContext)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
    }

    public async Task<ApiResponse<OrderDto>> CreateOrderAsync(int customerId, CreateOrderRequest request)
    {
        // 0. Validate Customer
        var customer = await _context.Users.FindAsync(customerId);
        if (customer == null)
        {
            return ApiResponse<OrderDto>.FailureResult("Customer account was not found.");
        }

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

        bool isPickup = !string.IsNullOrWhiteSpace(request.DeliveryLocation) &&
            (request.DeliveryLocation.Contains("Pickup", StringComparison.OrdinalIgnoreCase) ||
             request.DeliveryLocation.Contains("Counter", StringComparison.OrdinalIgnoreCase) ||
             request.DeliveryLocation.Contains("Dine-In", StringComparison.OrdinalIgnoreCase) ||
             request.DeliveryLocation.Contains("DineIn", StringComparison.OrdinalIgnoreCase));

        string orderType = !string.IsNullOrWhiteSpace(request.OrderType)
            ? request.OrderType.Trim()
            : (isPickup ? "Pickup" : "Delivery");

        if (request.DeliveryLocation.Contains("Dine-In", StringComparison.OrdinalIgnoreCase) ||
            request.DeliveryLocation.Contains("DineIn", StringComparison.OrdinalIgnoreCase) ||
            orderType.Equals("DineIn", StringComparison.OrdinalIgnoreCase))
        {
            orderType = "DineIn";
        }

        // Delivery fee applies ONLY to Delivery orders
        decimal deliveryFee = orderType.Equals("Delivery", StringComparison.OrdinalIgnoreCase) ? cafe.DeliveryFee : 0.00m;

        // Packaging fee: DineIn is always 0 (plates). StandardFree is 0 (festal/paper). OwnContainer is 0. TakeawayBox charges cafe fee.
        decimal packagingFee = 0.00m;
        if (!orderType.Equals("DineIn", StringComparison.OrdinalIgnoreCase) && 
            request.PackagingOption == PackagingOption.TakeawayBox)
        {
            packagingFee = cafe.TakeawayPackagingFee;
        }

        decimal discountAmount = 0m;
        decimal totalAmount = subtotal + deliveryFee + packagingFee;

        // 4. Meal Contract Handling (with Multi-Student Split & Smart Upgrade Math)
        StudentContract? activeContract = null;
        var coDinerRecords = new List<(User student, StudentContract contract, decimal upgradePortion)>();
        int hostCreditsToDeduct = 0;

        if (request.PaymentMethod == PaymentMethod.MealContract)
        {
            activeContract = await _context.StudentContracts
                .Include(sc => sc.ContractPlan)
                .Where(sc => sc.StudentId == customerId && 
                             sc.ContractPlan.CafeId == request.CafeId && 
                             sc.EndDate >= DateTime.UtcNow)
                .OrderByDescending(sc => sc.Status == ContractStatus.Active)
                .ThenBy(sc => sc.EndDate)
                .FirstOrDefaultAsync();

            if (activeContract == null)
            {
                return ApiResponse<OrderDto>.FailureResult("You do not have an active meal subscription plan with this café.");
            }

            if (activeContract.Status == ContractStatus.Depleted)
            {
                return ApiResponse<OrderDto>.FailureResult("Your meal contract credits and overdraft allowance are completely depleted.");
            }

            int hostAvailableCredits = activeContract.RemainingCredits + (activeContract.AllowOverdraft ? activeContract.OverdraftLimit : 0);
            if (hostAvailableCredits < 1)
            {
                return ApiResponse<OrderDto>.FailureResult("Your meal contract credits and overdraft allowance are completely depleted.");
            }

            // A) Validate Co-Diners (if any)
            var validatedCoDiners = new List<(User student, StudentContract contract)>();
            if (request.CoDinerUsernames != null && request.CoDinerUsernames.Any())
            {
                var distinctUsernames = request.CoDinerUsernames
                    .Select(u => u.Trim().ToLowerInvariant())
                    .Where(u => !string.IsNullOrEmpty(u) && u != customer.Username.ToLower())
                    .Distinct()
                    .ToList();

                foreach (var username in distinctUsernames)
                {
                    var buddy = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username);
                    if (buddy == null)
                    {
                        return ApiResponse<OrderDto>.FailureResult($"Co-diner '@{username}' was not found.");
                    }

                    var buddyContract = await _context.StudentContracts
                        .Include(sc => sc.ContractPlan)
                        .Where(sc => sc.StudentId == buddy.Id && 
                                     sc.ContractPlan.CafeId == request.CafeId && 
                                     sc.EndDate >= DateTime.UtcNow)
                        .OrderByDescending(sc => sc.Status == ContractStatus.Active)
                        .ThenBy(sc => sc.EndDate)
                        .FirstOrDefaultAsync();

                    if (buddyContract == null || buddyContract.Status == ContractStatus.Depleted)
                    {
                        return ApiResponse<OrderDto>.FailureResult($"Co-diner '@{username}' does not have an active pass at this café.");
                    }

                    int buddyAvailable = buddyContract.RemainingCredits + (buddyContract.AllowOverdraft ? buddyContract.OverdraftLimit : 0);
                    if (buddyAvailable < 1)
                    {
                        return ApiResponse<OrderDto>.FailureResult($"Co-diner '@{username}' has exhausted all pass credits.");
                    }

                    validatedCoDiners.Add((buddy, buddyContract));
                }
            }

            // B) Determine portions & Apply Smart Upgrade Math
            int totalPassCredits = 1 + validatedCoDiners.Count;
            decimal baseValue = activeContract.ContractPlan.BaseMealCreditValue;

            // Expand all ordered items into individual portion unit prices sorted descending
            var portions = new List<decimal>();
            foreach (var oi in orderItems)
            {
                for (int i = 0; i < oi.Quantity; i++)
                {
                    portions.Add(oi.UnitPrice);
                }
            }
            portions = portions.OrderByDescending(p => p).ToList();

            discountAmount = 0m;
            decimal totalUpgradeAndExtraCash = 0m;

            for (int i = 0; i < portions.Count; i++)
            {
                var price = portions[i];
                if (i < totalPassCredits)
                {
                    // Covered by pass credit (Host or Co-Diner)
                    decimal coveredPortion = Math.Min(price, baseValue);
                    discountAmount += coveredPortion;
                    decimal upgradePortion = price - coveredPortion;
                    totalUpgradeAndExtraCash += upgradePortion;

                    if (i == 0)
                    {
                        hostCreditsToDeduct = 1;
                    }
                    else
                    {
                        var (buddy, buddyContract) = validatedCoDiners[i - 1];
                        coDinerRecords.Add((buddy, buddyContract, upgradePortion));
                    }
                }
                else
                {
                    // Extra meal beyond pass credits: full price cash
                    totalUpgradeAndExtraCash += price;
                }
            }

            totalAmount = totalUpgradeAndExtraCash + deliveryFee + packagingFee;

            // Deduct from Host
            activeContract.RemainingCredits -= hostCreditsToDeduct;
            if (activeContract.RemainingCredits <= 0 && !activeContract.AllowOverdraft)
            {
                activeContract.Status = ContractStatus.Depleted;
            }

            // Deduct from Co-Diners
            foreach (var (_, buddyContract, _) in coDinerRecords)
            {
                buddyContract.RemainingCredits -= 1;
                if (buddyContract.RemainingCredits <= 0 && !buddyContract.AllowOverdraft)
                {
                    buddyContract.Status = ContractStatus.Depleted;
                }
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
            OrderType = orderType,
            PackagingOption = request.PackagingOption,
            PackagingFee = packagingFee,
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

        // 6. Record Contract Usage Logs & Co-Diner Records (Immutable Audit Ledger)
        if (activeContract != null && hostCreditsToDeduct > 0)
        {
            var usageLog = new ContractUsageLog
            {
                StudentContractId = activeContract.Id,
                OrderId = order.Id,
                CreditsDeducted = hostCreditsToDeduct,
                RemainingCreditsAfter = activeContract.RemainingCredits,
                UsedAt = DateTime.UtcNow
            };
            await _context.ContractUsageLogs.AddAsync(usageLog);
        }

        foreach (var (buddy, buddyContract, upgradePortion) in coDinerRecords)
        {
            var coDinerEntry = new OrderCoDiner
            {
                OrderId = order.Id,
                StudentId = buddy.Id,
                StudentContractId = buddyContract.Id,
                CreditsDeducted = 1,
                UpgradeCashPortion = upgradePortion,
                CreatedAt = DateTime.UtcNow
            };
            await _context.OrderCoDiners.AddAsync(coDinerEntry);

            var buddyUsageLog = new ContractUsageLog
            {
                StudentContractId = buddyContract.Id,
                OrderId = order.Id,
                CreditsDeducted = 1,
                RemainingCreditsAfter = buddyContract.RemainingCredits,
                UsedAt = DateTime.UtcNow
            };
            await _context.ContractUsageLogs.AddAsync(buddyUsageLog);

            // Instant SignalR push alert to the co-diner's phone
            await _hubContext.Clients.Group($"user_{buddy.Id}").SendAsync("PassDeductedForGroupOrder", new
            {
                OrderNumber = order.OrderNumber,
                CafeName = cafe.Name,
                HostName = customer.FullName,
                UpgradeCashPortion = upgradePortion,
                RemainingCredits = buddyContract.RemainingCredits
            });
        }

        if (activeContract != null || coDinerRecords.Any())
        {
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Order {OrderNumber} created successfully by Customer {CustomerId} at Cafe {CafeId}", order.OrderNumber, customerId, request.CafeId);

        var createdResult = await GetOrderByIdAsync(order.Id);
        if (createdResult.Success && createdResult.Data != null)
        {
            // 1. Live Push: Alert the Café Kitchen screen in real-time
            await _hubContext.Clients.Group($"cafe_{order.CafeId}").SendAsync("OrderCreated", createdResult.Data);

            // 2. Live Push: If delivery requested, alert available couriers
            if (order.DeliveryFee > 0)
            {
                await _hubContext.Clients.Group("couriers").SendAsync("DeliveryAvailable", createdResult.Data);
            }
        }

        return createdResult;
    }

    public async Task<ApiResponse<List<OrderDto>>> GetOrdersForCustomerAsync(int customerId)
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Cafe)
            .Include(o => o.Courier)
            .Include(o => o.OrderItems)
            .Include(o => o.CoDiners).ThenInclude(cd => cd.Student)
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
            .Include(o => o.CoDiners).ThenInclude(cd => cd.Student)
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
            .Include(o => o.CoDiners).ThenInclude(cd => cd.Student)
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

        var updatedResult = await GetOrderByIdAsync(orderId);
        if (updatedResult.Success && updatedResult.Data != null)
        {
            // 1. Live Push: Alert the student tracking this specific order
            await _hubContext.Clients.Group($"order_{orderId}").SendAsync("OrderStatusChanged", updatedResult.Data);

            // 2. Live Push: Alert the café staff / kitchen display
            await _hubContext.Clients.Group($"cafe_{order.CafeId}").SendAsync("OrderUpdated", updatedResult.Data);

            // 3. Live Push: If order is ready and is a delivery, alert couriers
            if (newStatus == OrderStatus.ReadyForPickup && order.DeliveryFee > 0)
            {
                await _hubContext.Clients.Group("couriers").SendAsync("DeliveryAvailable", updatedResult.Data);
            }
        }

        return updatedResult;
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

        var claimedResult = await GetOrderByIdAsync(orderId);
        if (claimedResult.Success && claimedResult.Data != null)
        {
            // 1. Live Push: Notify the student their order is on the way
            await _hubContext.Clients.Group($"order_{orderId}").SendAsync("OrderStatusChanged", claimedResult.Data);

            // 2. Live Push: Notify the café kitchen that courier has claimed the order
            await _hubContext.Clients.Group($"cafe_{order.CafeId}").SendAsync("OrderUpdated", claimedResult.Data);
        }

        return claimedResult;
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
            PackagingFee = o.PackagingFee,
            DiscountAmount = o.DiscountAmount,
            TotalAmount = o.TotalAmount,
            OrderType = o.OrderType,
            PackagingOption = o.PackagingOption,
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
            }).ToList(),
            CoDiners = o.CoDiners.Select(cd => new CoDinerDto
            {
                StudentId = cd.StudentId,
                Username = cd.Student?.Username ?? string.Empty,
                FullName = cd.Student?.FullName ?? string.Empty,
                CreditsDeducted = cd.CreditsDeducted,
                UpgradeCashPortion = cd.UpgradeCashPortion
            }).ToList()
        };
    }
}
