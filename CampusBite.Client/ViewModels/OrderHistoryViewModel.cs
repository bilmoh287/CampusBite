using System.Globalization;

namespace CampusBite.ViewModels;

public partial class OrderHistoryViewModel : ObservableObject
{
    private const string BudgetPrefKey = "StudentMonthlyBudget";
    private readonly IOrderService _orderService;

    [ObservableProperty]
    private ObservableCollection<Order> _orders = new();

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isEmpty = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RemainingBudget))]
    [NotifyPropertyChangedFor(nameof(BudgetProgress))]
    [NotifyPropertyChangedFor(nameof(BudgetPercentText))]
    [NotifyPropertyChangedFor(nameof(BudgetStatusText))]
    [NotifyPropertyChangedFor(nameof(BudgetProgressColor))]
    private decimal _monthlyBudget = 100.00m;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RemainingBudget))]
    [NotifyPropertyChangedFor(nameof(BudgetProgress))]
    [NotifyPropertyChangedFor(nameof(BudgetPercentText))]
    [NotifyPropertyChangedFor(nameof(BudgetStatusText))]
    [NotifyPropertyChangedFor(nameof(BudgetProgressColor))]
    private decimal _currentMonthSpent = 0.00m;

    [ObservableProperty]
    private decimal _totalSpentAllTime = 0.00m;

    public decimal RemainingBudget => Math.Max(0, MonthlyBudget - CurrentMonthSpent);

    public double BudgetProgress => MonthlyBudget > 0 
        ? Math.Clamp((double)(CurrentMonthSpent / MonthlyBudget), 0.0, 1.0) 
        : 0.0;

    public string BudgetPercentText => MonthlyBudget > 0 
        ? $"{Math.Round((CurrentMonthSpent / MonthlyBudget) * 100)}%" 
        : "0%";

    public string BudgetStatusText
    {
        get
        {
            if (MonthlyBudget <= 0) return "No Budget Set";
            if (CurrentMonthSpent > MonthlyBudget) return "⚠️ Budget Exceeded!";
            if (BudgetProgress >= 0.8) return "⏳ Nearing Limit";
            return "👍 On Track";
        }
    }

    public string BudgetProgressColor
    {
        get
        {
            if (CurrentMonthSpent > MonthlyBudget) return "#F44336"; // Red
            if (BudgetProgress >= 0.8) return "#FF9800"; // Amber
            return "#4CAF50"; // Green
        }
    }

    public bool HasOrders => !IsEmpty;

    public OrderHistoryViewModel(IOrderService orderService)
    {
        _orderService = orderService;
        LoadBudgetPreference();
    }

    private void LoadBudgetPreference()
    {
        double savedBudget = Preferences.Get(BudgetPrefKey, 100.0);
        MonthlyBudget = (decimal)savedBudget;
    }

    private bool _isProcessing;

    [RelayCommand]
    public async Task LoadOrdersAsync()
    {
        if (_isProcessing) return;

        try
        {
            _isProcessing = true;
            IsRefreshing = true;

            var list = await _orderService.GetOrdersAsync();
            Orders = new ObservableCollection<Order>(list);
            IsEmpty = Orders.Count == 0;
            OnPropertyChanged(nameof(HasOrders));

            RecalculateSpending();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to load orders: {ex.Message}", "OK");
        }
        finally
        {
            _isProcessing = false;
            IsRefreshing = false;
        }
    }

    private void RecalculateSpending()
    {
        var now = DateTime.Now;
        TotalSpentAllTime = Orders.Sum(o => o.TotalAmount);
        CurrentMonthSpent = Orders
            .Where(o => o.OrderDate.Year == now.Year && o.OrderDate.Month == now.Month)
            .Sum(o => o.TotalAmount);

        OnPropertyChanged(nameof(RemainingBudget));
        OnPropertyChanged(nameof(BudgetProgress));
        OnPropertyChanged(nameof(BudgetPercentText));
        OnPropertyChanged(nameof(BudgetStatusText));
        OnPropertyChanged(nameof(BudgetProgressColor));
    }

    [RelayCommand]
    public async Task SetBudgetAsync()
    {
        string result = await Shell.Current.DisplayPromptAsync(
            "Monthly Food Budget 🎯",
            "Set your monthly campus meal allowance:",
            initialValue: MonthlyBudget.ToString("F0"),
            maxLength: 6,
            keyboard: Keyboard.Numeric,
            accept: "Save",
            cancel: "Cancel");

        if (string.IsNullOrWhiteSpace(result)) return;

        if (!decimal.TryParse(result, NumberStyles.Currency | NumberStyles.Number, CultureInfo.InvariantCulture, out decimal newBudget) &&
            !decimal.TryParse(result, out newBudget))
        {
            await Shell.Current.DisplayAlertAsync("Invalid Budget", "Please enter a valid numeric budget (e.g. 100).", "OK");
            return;
        }

        if (newBudget <= 0)
        {
            await Shell.Current.DisplayAlertAsync("Invalid Budget", "Budget must be greater than $0.", "OK");
            return;
        }

        MonthlyBudget = newBudget;
        Preferences.Set(BudgetPrefKey, (double)newBudget);
        RecalculateSpending();

        await Shell.Current.DisplayAlertAsync("Budget Saved 🎯", $"Your monthly campus food budget is now ${newBudget:F2}.", "Great!");
    }

    [RelayCommand]
    private async Task SelectOrderAsync(Order order)
    {
        if (order == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "Order", order }
        };

        await Shell.Current.GoToAsync(nameof(OrderDetailsPage), navigationParameter);
    }
}
