using System.Collections.ObjectModel;
using CampusBite.Models;
using CampusBite.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CampusBite.ViewModels;

[QueryProperty(nameof(CafeId), "CafeId")]
public partial class MealPlansViewModel : ObservableObject
{
    private readonly IContractService _contractService;
    private readonly ICafeService _cafeService;

    [ObservableProperty]
    private int _cafeId;

    [ObservableProperty]
    private Cafe? _selectedCafe;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveContract))]
    [NotifyPropertyChangedFor(nameof(IsInDebt))]
    [NotifyPropertyChangedFor(nameof(DebtAmountText))]
    private StudentContractDto? _activeContract;

    public bool HasActiveContract => ActiveContract != null;
    public bool IsInDebt => ActiveContract?.IsInDebt ?? false;
    public string DebtAmountText => ActiveContract != null && ActiveContract.IsInDebt 
        ? $"{ActiveContract.DebtMealsCount} Meals in Overdraft" 
        : string.Empty;

    public ObservableCollection<Cafe> Cafes { get; } = new();
    public ObservableCollection<ContractPlanDto> AvailablePlans { get; } = new();
    public ObservableCollection<StudentContractDto> MyContracts { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshing;

    public MealPlansViewModel(IContractService contractService, ICafeService cafeService)
    {
        _contractService = contractService;
        _cafeService = cafeService;
    }

    async partial void OnCafeIdChanged(int value)
    {
        if (value > 0)
        {
            await LoadDataAsync(value);
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsRefreshing = true;
        try
        {
            await LoadDataAsync(SelectedCafe?.Id ?? CafeId);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task LoadDataAsync(int? targetCafeId = null)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            // 1. Load Cafés if not already loaded
            var cafesList = await _cafeService.GetCafesAsync();
            Cafes.Clear();
            foreach (var c in cafesList)
            {
                Cafes.Add(c);
            }

            // Determine target cafe
            if (targetCafeId.HasValue && targetCafeId.Value > 0)
            {
                SelectedCafe = Cafes.FirstOrDefault(c => c.Id == targetCafeId.Value);
            }
            else if (SelectedCafe == null && Cafes.Count > 0)
            {
                SelectedCafe = Cafes[0];
            }

            // 2. Load Student's All Active Contracts
            var contracts = await _contractService.GetMyContractsAsync();
            MyContracts.Clear();
            foreach (var contract in contracts)
            {
                MyContracts.Add(contract);
            }

            // 3. Update Active Contract for selected cafe
            if (SelectedCafe != null)
            {
                ActiveContract = MyContracts.FirstOrDefault(c => c.CafeId == SelectedCafe.Id && c.IsActive);
                await LoadPlansForCafeAsync(SelectedCafe.Id);
            }
            else
            {
                ActiveContract = null;
                AvailablePlans.Clear();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SelectCafeAsync(Cafe cafe)
    {
        if (cafe == null || SelectedCafe?.Id == cafe.Id) return;

        SelectedCafe = cafe;
        CafeId = cafe.Id;
        ActiveContract = MyContracts.FirstOrDefault(c => c.CafeId == cafe.Id && c.IsActive);
        await LoadPlansForCafeAsync(cafe.Id);
    }

    private async Task LoadPlansForCafeAsync(int cafeId)
    {
        try
        {
            var plans = await _contractService.GetPlansByCafeAsync(cafeId);
            AvailablePlans.Clear();
            foreach (var plan in plans)
            {
                AvailablePlans.Add(plan);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Could not load meal plans: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task SubscribeAsync(ContractPlanDto plan)
    {
        if (plan == null || IsBusy) return;

        var isTopUp = HasActiveContract && ActiveContract?.CafeId == plan.CafeId;
        var confirmTitle = isTopUp ? "Top-Up Meal Credits" : "Activate Dining Pass";
        var confirmMsg = isTopUp
            ? $"Add {plan.TotalCredits} meals to your active balance for {plan.Price:F2} ETB?\n\n• New credits will roll over into your existing balance.\n• Validity extended by +{plan.ValidityDays} days."
            : $"Subscribe to {plan.Name} for {plan.Price:F2} ETB?\n\n• {plan.TotalCredits} meals ({plan.PricePerMeal:F2} ETB/meal)\n• Valid for {plan.ValidityDays} days.";

        var confirmed = await Shell.Current.DisplayAlertAsync(confirmTitle, confirmMsg, "Confirm Pass", "Cancel");
        if (!confirmed) return;

        try
        {
            IsBusy = true;
            var newContract = await _contractService.SubscribeToPlanAsync(plan.Id);
            if (newContract != null)
            {
                ActiveContract = newContract;

                // Refresh all student contracts
                var contracts = await _contractService.GetMyContractsAsync();
                MyContracts.Clear();
                foreach (var c in contracts)
                {
                    MyContracts.Add(c);
                }

                await Shell.Current.DisplayAlertAsync(
                    "Pass Activated! 🎉", 
                    $"Your dining pass at {newContract.CafeName} now has {newContract.RemainingCredits} meals available!\nExpires: {newContract.EndDate:MMM dd, yyyy}", 
                    "Awesome");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Error", "Could not complete pass subscription. Please try again.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Subscription Failed", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
