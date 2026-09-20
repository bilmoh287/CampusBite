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

    public CafeAdminViewModel(ICafeAdminService adminService)
    {
        _adminService = adminService;
    }

    partial void OnSelectedCafeChanged(Cafe? value)
    {
        if (value != null)
        {
            _ = LoadOrdersAsync();
        }
    }

    [RelayCommand]
    public async Task LoadAdminDataAsync()
    {
        IsRefreshing = true;

        try
        {
            var cafeList = await _adminService.GetCafesAsync();
            Cafes = new ObservableCollection<Cafe>(cafeList);

            if (SelectedCafe == null && Cafes.Any())
            {
                SelectedCafe = Cafes.First();
            }
            else if (SelectedCafe != null)
            {
                await LoadOrdersAsync();
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to load admin data: {ex.Message}", "OK");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task LoadOrdersAsync()
    {
        if (SelectedCafe == null) return;

        IsRefreshing = true;
        try
        {
            var orders = await _adminService.GetCafeOrdersAsync(SelectedCafe.Id, SelectedStatusFilter);
            IncomingOrders = new ObservableCollection<Order>(orders);
            IsEmpty = IncomingOrders.Count == 0;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to load orders: {ex.Message}", "OK");
        }
        finally
        {
            IsRefreshing = false;
        }
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
