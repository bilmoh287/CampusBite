using System.Collections.ObjectModel;
using CampusBite.Models;
using CampusBite.Models.UI;
using CampusBite.Services.Interfaces;

namespace CampusBite.Services.Implementations;

public class CartService : ICartService
{
    public ObservableCollection<CartItem> Items { get; } = new();

    public decimal TotalAmount => Items.Sum(i => i.TotalPrice);

    public int TotalCount => Items.Sum(i => i.Quantity);

    public int? CafeId => Items.FirstOrDefault()?.Product.CafeId;

    public async Task<bool> AddToCartAsync(Product product, int quantity)
    {
        if (product == null || quantity <= 0) return false;

        // Check for Multi-Café Cart conflict
        if (CafeId.HasValue && product.CafeId != CafeId.Value)
        {
            bool clear = await Shell.Current.DisplayAlertAsync(
                "Different Café",
                "Your cart contains items from another café. Clear cart and add this item instead?",
                "Clear & Add",
                "Cancel");

            if (clear)
            {
                ClearCart();
            }
            else
            {
                return false;
            }
        }

        // Check if product already exists in cart
        var existingItem = Items.FirstOrDefault(i => i.Product.Id == product.Id);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            Items.Add(new CartItem
            {
                Product = product,
                Quantity = quantity
            });
        }

        return true;
    }

    public void RemoveFromCart(CartItem item)
    {
        if (item != null && Items.Contains(item))
        {
            Items.Remove(item);
        }
    }

    public void UpdateQuantity(CartItem item, int newQuantity)
    {
        if (item == null) return;

        if (newQuantity <= 0)
        {
            RemoveFromCart(item);
        }
        else
        {
            item.Quantity = newQuantity;
        }
    }

    public void ClearCart()
    {
        Items.Clear();
    }
}
