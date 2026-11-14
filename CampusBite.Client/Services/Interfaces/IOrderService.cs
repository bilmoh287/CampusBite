namespace CampusBite.Services.Interfaces;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(int cafeId, string orderType, string? deliveryLocation, string? notes, IEnumerable<CartItem> cartItems, CampusBite.Shared.Enums.PaymentMethod paymentMethod = CampusBite.Shared.Enums.PaymentMethod.Cash);
    Task<List<Order>> GetOrdersAsync();
    Task<Order?> GetOrderByIdAsync(int orderId);
    Task<bool> CancelOrderAsync(int orderId);
}
