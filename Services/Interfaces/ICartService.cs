using System.Collections.ObjectModel;
using CampusBite.Models;
using CampusBite.Models.UI;

namespace CampusBite.Services.Interfaces;

public interface ICartService
{
    ObservableCollection<CartItem> Items { get; }
    decimal TotalAmount { get; }
    int TotalCount { get; }
    int? CafeId { get; }

    Task<bool> AddToCartAsync(Product product, int quantity);
    void RemoveFromCart(CartItem item);
    void UpdateQuantity(CartItem item, int newQuantity);
    void ClearCart();
}
