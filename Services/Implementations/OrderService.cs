using Microsoft.EntityFrameworkCore;

namespace CampusBite.Services.Implementations;

public class OrderService : IOrderService
{
    private readonly AppDbContext _dbContext;

    public OrderService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Order> CreateOrderAsync(int cafeId, string orderType, string? deliveryLocation, string? notes, IEnumerable<CartItem> cartItems)
    {
        var itemsList = cartItems.ToList();
        if (!itemsList.Any())
        {
            throw new InvalidOperationException("Cannot create an order from an empty cart.");
        }

        decimal subtotal = itemsList.Sum(i => i.TotalPrice);
        decimal deliveryFee = string.Equals(orderType, "Delivery", StringComparison.OrdinalIgnoreCase) ? 2.50m : 0.00m;
        decimal totalAmount = subtotal + deliveryFee;

        var order = new Order
        {
            CafeId = cafeId,
            OrderDate = DateTime.Now,
            TotalAmount = totalAmount,
            Status = "Pending",
            OrderType = orderType,
            DeliveryLocation = string.IsNullOrWhiteSpace(deliveryLocation) ? null : deliveryLocation.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };

        foreach (var cartItem in itemsList)
        {
            order.OrderItems.Add(new OrderItem
            {
                ProductId = cartItem.Product.Id,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.Product.Price
            });
        }

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        return order;
    }

    public async Task<List<Order>> GetOrdersAsync()
    {
        return await _dbContext.Orders
            .Include(o => o.Cafe)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.OrderDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Order?> GetOrderByIdAsync(int orderId)
    {
        return await _dbContext.Orders
            .Include(o => o.Cafe)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }
}
