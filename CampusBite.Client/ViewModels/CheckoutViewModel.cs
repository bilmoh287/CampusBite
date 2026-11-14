using System.Collections.ObjectModel;
using CampusBite.Models.UI;
using CampusBite.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CampusBite.ViewModels;

public partial class CheckoutViewModel : ObservableObject
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private readonly IContractService _contractService;

    [ObservableProperty]
    private string _selectedOrderType = "Pickup";

    [ObservableProperty]
    private string _deliveryLocation = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotProcessing))]
    private bool _isProcessing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCashSelected))]
    [NotifyPropertyChangedFor(nameof(IsContractSelected))]
    [NotifyPropertyChangedFor(nameof(CoveredMealsCount))]
    [NotifyPropertyChangedFor(nameof(DiscountAmount))]
    [NotifyPropertyChangedFor(nameof(TotalAmount))]
    [NotifyPropertyChangedFor(nameof(PerPersonShareText))]
    [NotifyPropertyChangedFor(nameof(HasGroupOrder))]
    private string _selectedPaymentMethod = "Cash"; // "Cash" or "MealContract"

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveContract))]
    [NotifyPropertyChangedFor(nameof(AvailableCredits))]
    [NotifyPropertyChangedFor(nameof(ContractSummaryText))]
    [NotifyPropertyChangedFor(nameof(CoveredMealsCount))]
    [NotifyPropertyChangedFor(nameof(DiscountAmount))]
    [NotifyPropertyChangedFor(nameof(TotalAmount))]
    [NotifyPropertyChangedFor(nameof(PerPersonShareText))]
    [NotifyPropertyChangedFor(nameof(HasGroupOrder))]
    private StudentContractDto? _activeContract;

    public ObservableCollection<CartItem> CartItems => _cartService.Items;
    public decimal Subtotal => _cartService.TotalAmount;
    public decimal DeliveryFee => SelectedOrderType == "Delivery" ? 15.00m : 0.00m; // 15 ETB campus delivery fee

    public bool IsPickupSelected => SelectedOrderType == "Pickup";
    public bool IsDeliverySelected => SelectedOrderType == "Delivery";
    public bool IsNotProcessing => !IsProcessing;

    public bool IsCashSelected => SelectedPaymentMethod == "Cash";
    public bool IsContractSelected => SelectedPaymentMethod == "MealContract";

    public bool HasActiveContract => ActiveContract != null;
    public int AvailableCredits => ActiveContract?.TotalAvailableCredits ?? 0;
    public int TotalItemQuantity => CartItems.Sum(i => i.Quantity);

    public int CoveredMealsCount => IsContractSelected
        ? Math.Min(TotalItemQuantity, Math.Max(0, AvailableCredits))
        : 0;

    public decimal DiscountAmount
    {
        get
        {
            if (!IsContractSelected || AvailableCredits <= 0 || TotalItemQuantity == 0)
                return 0.00m;

            if (CoveredMealsCount >= TotalItemQuantity)
                return Subtotal; // 100% of food covered by dining pass!

            // Hybrid: discount covered meals proportionally
            decimal averagePrice = Subtotal / TotalItemQuantity;
            return Math.Round(averagePrice * CoveredMealsCount, 2);
        }
    }

    public decimal TotalAmount => Math.Max(0, (Subtotal - DiscountAmount) + DeliveryFee);

    public bool HasDiscount => DiscountAmount > 0;
    public bool HasGroupOrder => TotalItemQuantity > 1;

    public string PerPersonShareText
    {
        get
        {
            if (TotalItemQuantity <= 1) return string.Empty;
            decimal perPerson = Math.Round(TotalAmount / TotalItemQuantity, 2);
            return $"Group of {TotalItemQuantity}: ~{perPerson:F2} ETB / person via CBE Birr";
        }
    }

    public string ContractSummaryText
    {
        get
        {
            if (ActiveContract == null)
                return "No active dining pass with this café.";

            string debtText = ActiveContract.IsInDebt ? $" (In Debt: -{ActiveContract.DebtMealsCount} meals)" : "";
            return $"{ActiveContract.PlanName}: {ActiveContract.RemainingCredits} credits left{debtText}";
        }
    }

    public CheckoutViewModel(ICartService cartService, IOrderService orderService, IContractService contractService)
    {
        _cartService = cartService;
        _orderService = orderService;
        _contractService = contractService;
    }

    partial void OnSelectedOrderTypeChanged(string value)
    {
        OnPropertyChanged(nameof(DeliveryFee));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(IsPickupSelected));
        OnPropertyChanged(nameof(IsDeliverySelected));
        OnPropertyChanged(nameof(PerPersonShareText));
    }

    [RelayCommand]
    public async Task LoadActiveContractAsync()
    {
        try
        {
            int cafeId = _cartService.CafeId ?? 1;
            ActiveContract = await _contractService.GetActiveContractForCafeAsync(cafeId);
        }
        catch
        {
            ActiveContract = null;
        }
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
    private void SelectPaymentMethod(string method)
    {
        if (method == "Cash" || method == "MealContract")
        {
            SelectedPaymentMethod = method;
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

        if (IsContractSelected && (ActiveContract == null || AvailableCredits < 1))
        {
            await Shell.Current.DisplayAlertAsync(
                "No Credits Available",
                "You do not have enough meal contract credits or overdraft allowance with this café. Please select Cash or purchase a pass.",
                "OK");
            return;
        }

        IsProcessing = true;

        try
        {
            int cafeId = _cartService.CafeId ?? 1;
            var paymentMethod = IsContractSelected ? PaymentMethod.MealContract : PaymentMethod.Cash;

            var order = await _orderService.CreateOrderAsync(
                cafeId,
                SelectedOrderType,
                DeliveryLocation,
                Notes,
                _cartService.Items,
                paymentMethod
            );

            // Clear session cart
            _cartService.ClearCart();

            string paymentSummary = IsContractSelected
                ? $"Redeemed {CoveredMealsCount} Meal Credit(s)\nCash/Delivery Due: {order.TotalAmount:F2} ETB"
                : $"Payment: Cash on Delivery\nTotal: {order.TotalAmount:F2} ETB";

            await Shell.Current.DisplayAlertAsync(
                "Order Placed! 🍔",
                $"Your order #{order.Id} has been submitted successfully.\nOrder Type: {SelectedOrderType}\n{paymentSummary}",
                "Track Order"
            );

            // Navigate to Order History so student can immediately monitor order status live
            await Shell.Current.GoToAsync("//OrderHistoryPage");
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
