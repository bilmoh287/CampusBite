namespace CampusBite.ViewModels;

public partial class CafeAdminViewModel : ObservableObject
{
    private readonly ICafeAdminService _adminService;

    [ObservableProperty]
    private ObservableCollection<Cafe> _cafes = new();

    [ObservableProperty]
    private Cafe? _selectedCafe;

    [ObservableProperty]
    private ObservableCollection<Order> _incomingOrders = new();

    [ObservableProperty]
    private string _selectedStatusFilter = "All";

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isEmpty = true;

    [ObservableProperty]
    private bool _isCafeOpen = true;

    private bool _isBusy;
    private bool _isInitializing;

    public CafeAdminViewModel(ICafeAdminService adminService)
    {
        _adminService = adminService;
    }

    partial void OnSelectedCafeChanged(Cafe? value)
    {
        if (value != null)
        {
            _isCafeOpen = value.IsOpen;
            OnPropertyChanged(nameof(IsCafeOpen));

            if (!_isInitializing && !_isBusy)
            {
                _ = LoadOrdersAsync();
            }
        }
    }

    async partial void OnIsCafeOpenChanged(bool value)
    {
        if (SelectedCafe == null || SelectedCafe.IsOpen == value || _isInitializing || _isBusy) return;

        bool updated = await _adminService.ToggleCafeStatusAsync(SelectedCafe.Id, value);
        if (updated)
        {
            SelectedCafe.IsOpen = value;
            string status = value ? "OPEN" : "CLOSED";
            await Shell.Current.DisplayAlertAsync("Café Status Updated 🏪", $"{SelectedCafe.Name} is now marked {status} for student orders.", "OK");
        }
        else
        {
            // Revert if database update failed
            _isCafeOpen = SelectedCafe.IsOpen;
            OnPropertyChanged(nameof(IsCafeOpen));
        }
    }

    [RelayCommand]
    public async Task LoadAdminDataAsync()
    {
        if (_isBusy) return;

        try
        {
            _isBusy = true;
            _isInitializing = true;
            IsRefreshing = true;

            var cafeList = await _adminService.GetCafesAsync();
            Cafes = new ObservableCollection<Cafe>(cafeList);

            if (SelectedCafe == null && Cafes.Any())
            {
                SelectedCafe = Cafes.First();
            }

            _isInitializing = false;

            if (SelectedCafe != null)
            {
                await LoadOrdersInternalAsync();
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to load admin data: {ex.Message}", "OK");
        }
        finally
        {
            _isInitializing = false;
            _isBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task LoadOrdersAsync()
    {
        if (_isBusy || SelectedCafe == null) return;

        try
        {
            _isBusy = true;
            IsRefreshing = true;
            await LoadOrdersInternalAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to load orders: {ex.Message}", "OK");
        }
        finally
        {
            _isBusy = false;
            IsRefreshing = false;
        }
    }

    private async Task LoadOrdersInternalAsync()
    {
        if (SelectedCafe == null) return;

        var orders = await _adminService.GetCafeOrdersAsync(SelectedCafe.Id, SelectedStatusFilter);
        IncomingOrders = new ObservableCollection<Order>(orders);
        IsEmpty = IncomingOrders.Count == 0;
    }

    [RelayCommand]
    private async Task FilterByStatusAsync(string status)
    {
        SelectedStatusFilter = status;
        await LoadOrdersAsync();
    }

    [RelayCommand]
    private async Task UpdateStatusAsync(object parameter)
    {
        if (parameter is ValueTuple<Order, string> args)
        {
            await ExecuteStatusUpdate(args.Item1, args.Item2);
        }
        else if (parameter is Order order)
        {
            // Default step progression: Pending -> Preparing -> Ready -> Completed
            string nextStatus = order.Status switch
            {
                "Pending" => "Preparing",
                "Preparing" => "Ready",
                "Ready" => "Completed",
                _ => "Completed"
            };
            await ExecuteStatusUpdate(order, nextStatus);
        }
    }

    [RelayCommand]
    private async Task AdvanceStatusAsync(Order order)
    {
        if (order == null) return;
        string nextStatus = order.Status switch
        {
            "Pending" => "Preparing",
            "Preparing" => "Ready",
            "Ready" => "Completed",
            _ => "Completed"
        };
        await ExecuteStatusUpdate(order, nextStatus);
    }

    private async Task ExecuteStatusUpdate(Order order, string newStatus)
    {
        if (order == null || string.IsNullOrWhiteSpace(newStatus)) return;

        bool updated = await _adminService.UpdateOrderStatusAsync(order.Id, newStatus);
        if (updated)
        {
            order.Status = newStatus;
            await Shell.Current.DisplayAlertAsync("Order Updated 🔔", $"Order #{order.Id} status updated to '{newStatus}'.", "OK");
            await LoadOrdersAsync();
        }
    }
}
