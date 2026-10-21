namespace CampusBite.Services.Interfaces;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(int cafeId, string orderType, string? deliveryLocation, string? notes, IEnumerable<CartItem> cartItems);
    Task<List<Order>> GetOrdersAsync();
    Task<Order?> GetOrderByIdAsync(int orderId);
}
