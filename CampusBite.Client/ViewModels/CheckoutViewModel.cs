using System.Collections.ObjectModel;
using CampusBite.Models.UI;
using CampusBite.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CampusBite.ViewModels;

public class OrderCoverDetail
{
    public string ItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string CoveredBy { get; set; } = string.Empty;
    public decimal DiscountCovered { get; set; }
    public decimal CashDifference { get; set; }
    public string StatusBadge => CashDifference > 0 
        ? $"+{CashDifference:F0} ETB cash" 
        : (DiscountCovered > 0 ? "Covered (0 ETB)" : $"{UnitPrice:F0} ETB");
}

public partial class CheckoutViewModel : ObservableObject
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private readonly IContractService _contractService;
    private readonly IBuddyService _buddyService;
    private readonly ICafeService _cafeService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DeliveryFee))]
    [NotifyPropertyChangedFor(nameof(PackagingFee))]
    [NotifyPropertyChangedFor(nameof(TotalAmount))]
    [NotifyPropertyChangedFor(nameof(IsDineInSelected))]
    [NotifyPropertyChangedFor(nameof(IsPickupSelected))]
    [NotifyPropertyChangedFor(nameof(IsDeliverySelected))]
    [NotifyPropertyChangedFor(nameof(ShowPackagingChoice))]
    [NotifyPropertyChangedFor(nameof(PerPersonShareText))]
    private string _selectedOrderType = "DineIn";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PackagingFee))]
    [NotifyPropertyChangedFor(nameof(TotalAmount))]
    [NotifyPropertyChangedFor(nameof(IsStandardPackagingSelected))]
    [NotifyPropertyChangedFor(nameof(IsTakeawayBoxSelected))]
    [NotifyPropertyChangedFor(nameof(IsOwnContainerSelected))]
    private PackagingOption _selectedPackagingOption = PackagingOption.StandardFree;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PackagingFee))]
    [NotifyPropertyChangedFor(nameof(TotalAmount))]
    [NotifyPropertyChangedFor(nameof(ShowPackagingChoice))]
    [NotifyPropertyChangedFor(nameof(TakeawayBoxPriceText))]
    private decimal _cafePackagingFee = 25.00m;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DeliveryFee))]
    [NotifyPropertyChangedFor(nameof(TotalAmount))]
    private decimal _cafeDeliveryFee = 15.00m;

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

    [ObservableProperty]
    private string _buddySearchInput = string.Empty;

    public ObservableCollection<CartItem> CartItems => _cartService.Items;
    public ObservableCollection<BuddyDto> SavedBuddies { get; } = new();
    public ObservableCollection<string> SelectedCoDinerUsernames { get; } = new();
    public ObservableCollection<OrderCoverDetail> SplitCoverageDetails { get; } = new();

    public decimal Subtotal => _cartService.TotalAmount;
    public decimal DeliveryFee => IsDeliverySelected ? CafeDeliveryFee : 0.00m;
    public decimal PackagingFee => (!IsDineInSelected && SelectedPackagingOption == PackagingOption.TakeawayBox) ? CafePackagingFee : 0.00m;

    public bool IsDineInSelected => SelectedOrderType == "DineIn";
    public bool IsPickupSelected => SelectedOrderType == "Pickup";
    public bool IsDeliverySelected => SelectedOrderType == "Delivery";

    public bool IsStandardPackagingSelected => SelectedPackagingOption == PackagingOption.StandardFree;
    public bool IsTakeawayBoxSelected => SelectedPackagingOption == PackagingOption.TakeawayBox;
    public bool IsOwnContainerSelected => SelectedPackagingOption == PackagingOption.OwnContainer;

    // Progressive Disclosure: Only show container option if NOT Dine-In AND the cafe actually offers/charges for boxes
    public bool ShowPackagingChoice => !IsDineInSelected && CafePackagingFee > 0;
    public string TakeawayBoxPriceText => CafePackagingFee > 0 ? $"+{CafePackagingFee:F0} ETB" : "Free";

    public bool IsNotProcessing => !IsProcessing;
    public bool IsCashSelected => SelectedPaymentMethod == "Cash";
    public bool IsContractSelected => SelectedPaymentMethod == "MealContract";

    public bool HasActiveContract => ActiveContract != null;
    public int AvailableCredits => ActiveContract?.TotalAvailableCredits ?? 0;
    public int TotalItemQuantity => CartItems.Sum(i => i.Quantity);
    public bool HasCoDiners => SelectedCoDinerUsernames.Count > 0;
    public bool CanAddMoreCoDiners => SelectedCoDinerUsernames.Count < Math.Max(0, TotalItemQuantity - 1);

    public int CoveredMealsCount => SplitCoverageDetails.Count(d => d.DiscountCovered > 0);
    public decimal DiscountAmount => SplitCoverageDetails.Sum(d => d.DiscountCovered);
    public decimal TotalAmount => Math.Max(0, (Subtotal - DiscountAmount) + DeliveryFee + PackagingFee);

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
            return $"{ActiveContract.PlanName}: {ActiveContract.RemainingCredits} credits left (Base: {ActiveContract.BaseMealCreditValue:F0} ETB){debtText}";
        }
    }

    public CheckoutViewModel(
        ICartService cartService, 
        IOrderService orderService, 
        IContractService contractService,
        IBuddyService buddyService,
        ICafeService cafeService)
    {
        _cartService = cartService;
        _orderService = orderService;
        _contractService = contractService;
        _buddyService = buddyService;
        _cafeService = cafeService;

        // Smart User Habit Memory: Restore preferred dining mode and packaging choices
        _selectedOrderType = Preferences.Get("Pref_OrderType", "DineIn");
        string savedPkg = Preferences.Get("Pref_Packaging", "StandardFree");
        _selectedPackagingOption = Enum.TryParse<PackagingOption>(savedPkg, out var pkg) ? pkg : PackagingOption.StandardFree;
    }

    [RelayCommand]
    public void SelectOrderType(string type)
    {
        if (string.IsNullOrWhiteSpace(type)) return;
        SelectedOrderType = type;
        Preferences.Set("Pref_OrderType", type);
        OnPropertyChanged(nameof(DeliveryFee));
        OnPropertyChanged(nameof(PackagingFee));
        OnPropertyChanged(nameof(TotalAmount));
        RecalculateSplitMath();
    }

    [RelayCommand]
    public void SelectPackagingOption(string option)
    {
        if (Enum.TryParse<PackagingOption>(option, out var pkg))
        {
            SelectedPackagingOption = pkg;
            Preferences.Set("Pref_Packaging", pkg.ToString());
            OnPropertyChanged(nameof(PackagingFee));
            OnPropertyChanged(nameof(TotalAmount));
        }
    }

    [RelayCommand]
    public async Task LoadActiveContractAsync()
    {
        try
        {
            int cafeId = _cartService.CafeId ?? 1;
            var cafe = await _cafeService.GetCafeByIdAsync(cafeId);
            if (cafe != null)
            {
                CafePackagingFee = cafe.TakeawayPackagingFee;
                CafeDeliveryFee = cafe.DeliveryFee;
            }
        }
        catch
        {
            // Fallback to defaults
        }

        try
        {
            int cafeId = _cartService.CafeId ?? 1;
            ActiveContract = await _contractService.GetActiveContractForCafeAsync(cafeId);
        }
        catch
        {
            ActiveContract = null;
        }

        try
        {
            var buddiesResponse = await _buddyService.GetBuddiesAsync();
            if (buddiesResponse != null && buddiesResponse.Success && buddiesResponse.Data != null)
            {
                SavedBuddies.Clear();
                foreach (var b in buddiesResponse.Data)
                {
                    SavedBuddies.Add(b);
                }
            }
        }
        catch
        {
            // Ignore offline buddy load
        }

        RecalculateSplitMath();
    }

    [RelayCommand]
    public async Task AddCoDinerAsync(string? usernameToAdd = null)
    {
        var targetUsername = string.IsNullOrWhiteSpace(usernameToAdd) 
            ? BuddySearchInput?.Trim().TrimStart('@') 
            : usernameToAdd.Trim().TrimStart('@');

        if (string.IsNullOrWhiteSpace(targetUsername)) return;

        targetUsername = targetUsername.ToLowerInvariant();

        if (SelectedCoDinerUsernames.Contains(targetUsername))
        {
            await Shell.Current.DisplayAlertAsync("Already Added", $"@{targetUsername} is already tagged for this order.", "OK");
            return;
        }

        if (SelectedCoDinerUsernames.Count >= Math.Max(0, TotalItemQuantity - 1))
        {
            await Shell.Current.DisplayAlertAsync("Limit Reached", $"You have {TotalItemQuantity} meals in your cart. You cannot tag more than {Math.Max(0, TotalItemQuantity - 1)} friend(s).", "OK");
            return;
        }

        SelectedCoDinerUsernames.Add(targetUsername);
        BuddySearchInput = string.Empty;
        RecalculateSplitMath();
    }

    [RelayCommand]
    public void RemoveCoDiner(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return;
        var clean = username.Trim().TrimStart('@').ToLowerInvariant();
        SelectedCoDinerUsernames.Remove(clean);
        RecalculateSplitMath();
    }

    public void RecalculateSplitMath()
    {
        SplitCoverageDetails.Clear();

        if (CartItems.Count == 0)
        {
            OnPropertyChanged(nameof(DiscountAmount));
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(CoveredMealsCount));
            OnPropertyChanged(nameof(HasDiscount));
            OnPropertyChanged(nameof(HasCoDiners));
            OnPropertyChanged(nameof(CanAddMoreCoDiners));
            return;
        }

        var portions = CartItems.SelectMany(ci => Enumerable.Repeat(ci.Product, ci.Quantity))
            .OrderByDescending(p => p.Price)
            .ToList();

        decimal baseCredit = ActiveContract?.BaseMealCreditValue > 0 
            ? ActiveContract.BaseMealCreditValue 
            : 130.00m;

        bool hostHasPass = IsContractSelected && HasActiveContract && AvailableCredits > 0;
        int hostPassAvailable = hostHasPass ? 1 : 0;
        var availableCoDiners = new Queue<string>(SelectedCoDinerUsernames);

        foreach (var product in portions)
        {
            if (hostPassAvailable > 0)
            {
                hostPassAvailable--;
                decimal discount = Math.Min(product.Price, baseCredit);
                decimal cashDiff = product.Price - discount;

                SplitCoverageDetails.Add(new OrderCoverDetail
                {
                    ItemName = product.Name,
                    UnitPrice = product.Price,
                    CoveredBy = "Your Pass",
                    DiscountCovered = discount,
                    CashDifference = cashDiff
                });
            }
            else if (availableCoDiners.Count > 0)
            {
                var coDiner = availableCoDiners.Dequeue();
                decimal discount = Math.Min(product.Price, baseCredit);
                decimal cashDiff = product.Price - discount;

                SplitCoverageDetails.Add(new OrderCoverDetail
                {
                    ItemName = product.Name,
                    UnitPrice = product.Price,
                    CoveredBy = $"@{coDiner}'s Pass",
                    DiscountCovered = discount,
                    CashDifference = cashDiff
                });
            }
            else
            {
                SplitCoverageDetails.Add(new OrderCoverDetail
                {
                    ItemName = product.Name,
                    UnitPrice = product.Price,
                    CoveredBy = "Cash / Uncovered",
                    DiscountCovered = 0,
                    CashDifference = product.Price
                });
            }
        }

        OnPropertyChanged(nameof(DiscountAmount));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(CoveredMealsCount));
        OnPropertyChanged(nameof(HasDiscount));
        OnPropertyChanged(nameof(HasCoDiners));
        OnPropertyChanged(nameof(CanAddMoreCoDiners));
        OnPropertyChanged(nameof(PerPersonShareText));
    }


    [RelayCommand]
    private void SelectPaymentMethod(string method)
    {
        if (method == "Cash" || method == "MealContract")
        {
            SelectedPaymentMethod = method;
            RecalculateSplitMath();
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
                paymentMethod,
                SelectedCoDinerUsernames,
                SelectedPackagingOption
            );

            // Clear session cart
            _cartService.ClearCart();

            string passBreakdown = SelectedCoDinerUsernames.Count > 0
                ? $"\nSplit with: {string.Join(", ", SelectedCoDinerUsernames.Select(u => "@" + u))}"
                : "";

            string paymentSummary = IsContractSelected
                ? $"Redeemed {CoveredMealsCount} Meal Pass(es){passBreakdown}\nCash/Delivery Due: {order.TotalAmount:F2} ETB"
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
