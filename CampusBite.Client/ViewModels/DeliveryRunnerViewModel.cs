namespace CampusBite.ViewModels;

public partial class DeliveryRunnerViewModel : ObservableObject
{
    private readonly IDeliveryService _deliveryService;
    private bool _isBusy;

    [ObservableProperty]
    private ObservableCollection<Order> _deliveries = new();

    [ObservableProperty]
    private ObservableCollection<Cafe> _campusCafes = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFilterAll))]
    [NotifyPropertyChangedFor(nameof(IsFilterReady))]
    [NotifyPropertyChangedFor(nameof(IsFilterInTransit))]
    [NotifyPropertyChangedFor(nameof(IsFilterDelivered))]
    private string _selectedFilter = "All";

    public bool IsFilterAll => SelectedFilter == "All";
    public bool IsFilterReady => SelectedFilter == "Ready";
    public bool IsFilterInTransit => SelectedFilter == "Out for Delivery";
    public bool IsFilterDelivered => SelectedFilter == "Delivered";

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isEmpty = true;

    [ObservableProperty]
    private int _readyCount;

    [ObservableProperty]
    private int _inTransitCount;

    [ObservableProperty]
    private int _completedCount;

    [ObservableProperty]
    private decimal _earningsToday;

    public DeliveryRunnerViewModel(IDeliveryService deliveryService)
    {
        _deliveryService = deliveryService;
    }

    [RelayCommand]
    public async Task LoadDeliveriesAsync()
    {
        if (_isBusy) return;

        try
        {
            _isBusy = true;
            IsRefreshing = true;

            // 1. Fetch all deliveries to compute global dashboard metrics
            var allDeliveries = await _deliveryService.GetDeliveryOrdersAsync(null);
            ReadyCount = allDeliveries.Count(o => o.Status == "Ready");
            InTransitCount = allDeliveries.Count(o => o.Status == "Out for Delivery");
            CompletedCount = allDeliveries.Count(o => o.Status == "Delivered" || o.Status == "Completed");
            EarningsToday = CompletedCount * 2.50m;

            // 2. Filter deliveries based on active courier tab
            IEnumerable<Order> filteredList = SelectedFilter switch
            {
                "Ready" => allDeliveries.Where(o => o.Status == "Ready"),
                "Out for Delivery" => allDeliveries.Where(o => o.Status == "Out for Delivery"),
                "Delivered" => allDeliveries.Where(o => o.Status == "Delivered" || o.Status == "Completed"),
                _ => allDeliveries
            };

            Deliveries = new ObservableCollection<Order>(filteredList);
            IsEmpty = Deliveries.Count == 0;

            // 3. Load live campus cafes overview
            var cafes = await _deliveryService.GetCafesOverviewAsync();
            CampusCafes = new ObservableCollection<Cafe>(cafes);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to load deliveries: {ex.Message}", "OK");
        }
        finally
        {
            _isBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task SetFilterAsync(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter) || SelectedFilter == filter) return;

        SelectedFilter = filter;
        await LoadDeliveriesAsync();
    }

    [RelayCommand]
    public async Task AdvanceDeliveryStatusAsync(Order order)
    {
        if (order == null || _isBusy) return;

        if (order.Status == "Ready")
        {
            bool updated = await _deliveryService.UpdateDeliveryStatusAsync(order.Id, "Out for Delivery");
            if (updated)
            {
                order.Status = "Out for Delivery";
                await Shell.Current.DisplayAlertAsync(
                    "Order Picked Up! 🛵",
                    $"Order #{order.Id} picked up from {order.Cafe?.Name ?? "Cafe"}.\nDestination: {order.DeliveryLocation ?? "Campus Location"}",
                    "On My Way!");
                await LoadDeliveriesAsync();
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Error", "Failed to update order status.", "OK");
            }
        }
        else if (order.Status == "Out for Delivery")
        {
            bool updated = await _deliveryService.UpdateDeliveryStatusAsync(order.Id, "Delivered");
            if (updated)
            {
                order.Status = "Delivered";
                await Shell.Current.DisplayAlertAsync(
                    "Delivery Completed! 🏁",
                    $"Order #{order.Id} delivered successfully.\n+$2.50 added to your courier earnings today!",
                    "Awesome!");
                await LoadDeliveriesAsync();
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Error", "Failed to complete delivery.", "OK");
            }
        }
    }
}
