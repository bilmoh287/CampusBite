using CampusBite.Models;
using CampusBite.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CampusBite.ViewModels;

[QueryProperty(nameof(Product), "Product")]
public partial class FoodDetailsViewModel : ObservableObject
{
    private readonly ICartService _cartService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPrice))]
    private Product? _product;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPrice))]
    private int _quantity = 1;

    public decimal TotalPrice => (Product?.Price ?? 0m) * Quantity;

    public FoodDetailsViewModel(ICartService cartService)
    {
        _cartService = cartService;
    }

    [RelayCommand]
    private void IncreaseQuantity()
    {
        Quantity++;
    }

    [RelayCommand]
    private void DecreaseQuantity()
    {
        if (Quantity > 1)
        {
            Quantity--;
        }
    }

    [RelayCommand]
    private async Task AddToCartAsync()
    {
        if (Product == null) return;

        bool added = await _cartService.AddToCartAsync(Product, Quantity);
        if (added)
        {
            await Shell.Current.DisplayAlertAsync("Added to Cart 🛒", $"Added {Quantity}x '{Product.Name}' (${TotalPrice:F2}) to your cart.", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }
}
