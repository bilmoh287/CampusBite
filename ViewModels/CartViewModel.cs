using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CampusBite.Models;
using CampusBite.Models.UI;
using CampusBite.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CampusBite.ViewModels;

public partial class CartViewModel : ObservableObject
{
    private readonly ICartService _cartService;

    public ObservableCollection<CartItem> Items => _cartService.Items;

    public decimal TotalAmount => _cartService.TotalAmount;
    public int TotalCount => _cartService.TotalCount;
    public bool IsEmpty => _cartService.Items.Count == 0;
    public bool HasItems => !IsEmpty;

    public CartViewModel(ICartService cartService)
    {
        _cartService = cartService;

        // Listen for collection changes to recalculate totals and visibility
        _cartService.Items.CollectionChanged += OnCartItemsCollectionChanged;
    }

    private void OnCartItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshTotals();
    }

    public void RefreshTotals()
    {
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasItems));
    }

    [RelayCommand]
    private void RemoveItem(CartItem item)
    {
        if (item == null) return;
        _cartService.RemoveFromCart(item);
        RefreshTotals();
    }

    [RelayCommand]
    private void IncreaseQuantity(CartItem item)
    {
        if (item == null) return;
        _cartService.UpdateQuantity(item, item.Quantity + 1);
        RefreshTotals();
    }

    [RelayCommand]
    private void DecreaseQuantity(CartItem item)
    {
        if (item == null) return;
        _cartService.UpdateQuantity(item, item.Quantity - 1);
        RefreshTotals();
    }

    [RelayCommand]
    private async Task ClearCartAsync()
    {
        if (IsEmpty) return;

        bool confirm = await Shell.Current.DisplayAlertAsync("Clear Cart", "Are you sure you want to remove all items from your cart?", "Yes", "No");
        if (confirm)
        {
            _cartService.ClearCart();
            RefreshTotals();
        }
    }

    [RelayCommand]
    private async Task ProceedToCheckoutAsync()
    {
        if (IsEmpty)
        {
            await Shell.Current.DisplayAlertAsync("Cart Empty", "Please add items to your cart before proceeding to checkout.", "OK");
            return;
        }

        await Shell.Current.GoToAsync(nameof(CheckoutPage));
    }
}
