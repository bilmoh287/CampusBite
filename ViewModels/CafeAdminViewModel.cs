namespace CampusBite.ViewModels;

public partial class CafeAdminViewModel : ObservableObject
{
    private readonly ICafeAdminService _adminService;

    [ObservableProperty]
    private ObservableCollection<Cafe> _cafes = new();

    [ObservableProperty]
    private Cafe? _selectedCafe;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOrdersSectionActive))]
    [NotifyPropertyChangedFor(nameof(IsStockSectionActive))]
    private string _activeSection = "Orders";

    public bool IsOrdersSectionActive => ActiveSection == "Orders";
    public bool IsStockSectionActive => ActiveSection == "Stock";

    [ObservableProperty]
    private ObservableCollection<Product> _cafeProducts = new();

    [ObservableProperty]
    private bool _isStockEmpty = true;

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
                _ = LoadSectionDataAsync();
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
                await LoadProductsInternalAsync();
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

    public async Task LoadSectionDataAsync()
    {
        if (_isBusy || SelectedCafe == null) return;

        try
        {
            _isBusy = true;
            IsRefreshing = true;
            await LoadOrdersInternalAsync();
            await LoadProductsInternalAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to load data: {ex.Message}", "OK");
        }
        finally
        {
            _isBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task SwitchSectionAsync(string section)
    {
        if (string.IsNullOrWhiteSpace(section) || ActiveSection == section) return;
        ActiveSection = section;

        if (ActiveSection == "Stock" && CafeProducts.Count == 0)
        {
            await LoadProductsInternalAsync();
        }
        else if (ActiveSection == "Orders" && IncomingOrders.Count == 0)
        {
            await LoadOrdersInternalAsync();
        }
    }

    [RelayCommand]
    public async Task LoadProductsAsync()
    {
        if (_isBusy || SelectedCafe == null) return;

        try
        {
            _isBusy = true;
            IsRefreshing = true;
            await LoadProductsInternalAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to load products: {ex.Message}", "OK");
        }
        finally
        {
            _isBusy = false;
            IsRefreshing = false;
        }
    }

    private async Task LoadProductsInternalAsync()
    {
        if (SelectedCafe == null) return;
        var products = await _adminService.GetCafeProductsAsync(SelectedCafe.Id);
        CafeProducts = new ObservableCollection<Product>(products);
        IsStockEmpty = CafeProducts.Count == 0;
    }

    [RelayCommand]
    public async Task ToggleProductAvailabilityAsync(Product product)
    {
        if (product == null) return;

        bool targetStatus = !product.IsAvailable;
        bool updated = await _adminService.ToggleProductAvailabilityAsync(product.Id, targetStatus);
        if (updated)
        {
            product.IsAvailable = targetStatus;

            // Trigger CollectionView re-bind by replacing item in ObservableCollection
            int index = CafeProducts.IndexOf(product);
            if (index >= 0)
            {
                CafeProducts[index] = new Product
                {
                    Id = product.Id,
                    CafeId = product.CafeId,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Category = product.Category,
                    IsAvailable = targetStatus,
                    ImageUrl = product.ImageUrl,
                    Cafe = product.Cafe
                };
            }

            string badge = targetStatus ? "IN STOCK ✅" : "SOLD OUT ❌";
            await Shell.Current.DisplayAlertAsync("Inventory Updated", $"'{product.Name}' is now marked {badge}.", "OK");
        }
        else
        {
            await Shell.Current.DisplayAlertAsync("Error", "Failed to update item availability in database.", "OK");
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
        if (order == null || order.Status == "Completed") return;
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
            await LoadOrdersInternalAsync();
        }
    }
}
