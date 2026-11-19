using CampusBite.Models;
using CampusBite.Models.UI;
using CampusBite.Shared.Enums;

namespace CampusBite.Services.Interfaces;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(
        int cafeId, 
        string orderType, 
        string? deliveryLocation, 
        string? notes, 
        IEnumerable<CartItem> cartItems, 
        PaymentMethod paymentMethod = PaymentMethod.Cash,
        IEnumerable<string>? coDinerUsernames = null,
        PackagingOption packagingOption = PackagingOption.StandardFree);

    Task<List<Order>> GetOrdersAsync();
    Task<Order?> GetOrderByIdAsync(int orderId);
    Task<bool> CancelOrderAsync(int orderId);
}
