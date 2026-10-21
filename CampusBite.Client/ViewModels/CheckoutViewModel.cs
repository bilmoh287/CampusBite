namespace CampusBite.ViewModels;

public partial class CheckoutViewModel : ObservableObject
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;

    [ObservableProperty]
    private string _selectedOrderType = "Pickup";

    [ObservableProperty]
    private string _deliveryLocation = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotProcessing))]
    private bool _isProcessing;

    public ObservableCollection<CartItem> CartItems => _cartService.Items;
    public decimal Subtotal => _cartService.TotalAmount;
    public decimal DeliveryFee => SelectedOrderType == "Delivery" ? 2.50m : 0.00m;
    public decimal TotalAmount => Subtotal + DeliveryFee;

    public bool IsPickupSelected => SelectedOrderType == "Pickup";
    public bool IsDeliverySelected => SelectedOrderType == "Delivery";
    public bool IsNotProcessing => !IsProcessing;

    public CheckoutViewModel(ICartService cartService, IOrderService orderService)
    {
        _cartService = cartService;
        _orderService = orderService;
    }

    partial void OnSelectedOrderTypeChanged(string value)
    {
        OnPropertyChanged(nameof(DeliveryFee));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(IsPickupSelected));
        OnPropertyChanged(nameof(IsDeliverySelected));
    }

    [RelayCommand]
    private void SelectOrderType(string type)
    {
        if (type == "Pickup" || type == "Delivery")
        {
            SelectedOrderType = type;
        }
    }

    [RelayCommand]
    private async Task PlaceOrderAsync()
    {
        if (IsProcessing) return;

        if (_cartService.Items.Count == 0)
        {
            await Shell.Current.DisplayAlertAsync("Empty Cart", "Your cart is empty.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        if (IsDeliverySelected && string.IsNullOrWhiteSpace(DeliveryLocation))
        {
            await Shell.Current.DisplayAlertAsync("Missing Location", "Please enter a campus delivery location (e.g., Library 2nd Floor).", "OK");
            return;
        }

        IsProcessing = true;

        try
        {
            int cafeId = _cartService.CafeId ?? 1;

            var order = await _orderService.CreateOrderAsync(
                cafeId,
                SelectedOrderType,
                DeliveryLocation,
                Notes,
                _cartService.Items
            );

            // Clear session cart
            _cartService.ClearCart();

            await Shell.Current.DisplayAlertAsync(
                "Order Placed! 🍔",
                $"Your order #{order.Id} has been submitted successfully.\nOrder Type: {SelectedOrderType}\nTotal: ${order.TotalAmount:F2}",
                "Awesome!"
            );

            // Navigate back to Home (CafeListPage)
            await Shell.Current.GoToAsync("//CafeListPage");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to place order: {ex.Message}", "OK");
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
