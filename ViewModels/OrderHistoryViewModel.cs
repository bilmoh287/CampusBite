namespace CampusBite.ViewModels;

public partial class OrderHistoryViewModel : ObservableObject
{
    private readonly IOrderService _orderService;

    [ObservableProperty]
    private ObservableCollection<Order> _orders = new();

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isEmpty = true;

    public bool HasOrders => !IsEmpty;

    public OrderHistoryViewModel(IOrderService orderService)
    {
        _orderService = orderService;
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
