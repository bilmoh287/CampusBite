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
    [NotifyPropertyChangedFor(nameof(IsSalesSectionActive))]
    private string _activeSection = "Orders";

    public bool IsOrdersSectionActive => ActiveSection == "Orders";
    public bool IsStockSectionActive => ActiveSection == "Stock";
    public bool IsSalesSectionActive => ActiveSection == "Sales";

    [ObservableProperty]
    private decimal _todayRevenue;

    [ObservableProperty]
    private decimal _monthRevenue;

    [ObservableProperty]
    private int _totalCompletedOrders;

    [ObservableProperty]
    private decimal _averageTicketValue;

    [ObservableProperty]
    private int _pickupOrderCount;

    [ObservableProperty]
    private int _deliveryOrderCount;

    [ObservableProperty]
    private ObservableCollection<TopSellingItemDto> _topSellingItems = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoSalesData))]
    private bool _hasSalesData;

    public bool HasNoSalesData => !HasSalesData;

    [ObservableProperty]
    private decimal _monthExpenses;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsProfitable))]
    private decimal _netProfit;

    [ObservableProperty]
    private decimal _profitMarginPercent;

    [ObservableProperty]
    private ObservableCollection<Expense> _cafeExpenses = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoExpenses))]
    private bool _hasExpenses;

    public bool HasNoExpenses => !HasExpenses;
    public bool IsProfitable => NetProfit >= 0;

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

    [ObservableProperty]
    private string _announcementText = string.Empty;

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
            AnnouncementText = value.Announcement ?? string.Empty;

            if (!_isInitializing && !_isBusy)
            {
                _ = LoadSectionDataAsync();
            }
        }
    }

    [RelayCommand]
    public async Task SaveAnnouncementAsync()
    {
        if (SelectedCafe == null || _isBusy) return;

        bool updated = await _adminService.UpdateCafeAnnouncementAsync(SelectedCafe.Id, AnnouncementText);
        if (updated)
        {
            SelectedCafe.Announcement = string.IsNullOrWhiteSpace(AnnouncementText) ? null : AnnouncementText.Trim();
            await Shell.Current.DisplayAlertAsync("Broadcast Updated 📢", $"Announcement updated for {SelectedCafe.Name}. Students will see this at the top of the menu.", "OK");
        }
        else
        {
            await Shell.Current.DisplayAlertAsync("Error", "Failed to update announcement.", "OK");
        }
    }

    [RelayCommand]
    public async Task ClearAnnouncementAsync()
    {
        if (SelectedCafe == null || _isBusy) return;

        AnnouncementText = string.Empty;
        bool updated = await _adminService.UpdateCafeAnnouncementAsync(SelectedCafe.Id, null);
        if (updated)
        {
            SelectedCafe.Announcement = null;
            await Shell.Current.DisplayAlertAsync("Announcement Cleared 🧹", $"Active announcement removed from {SelectedCafe.Name}.", "OK");
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
            if (ActiveSection == "Sales")
            {
                await ComputeSalesMetricsInternalAsync();
            }
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
        else if (ActiveSection == "Sales")
        {
            await ComputeSalesMetricsAsync();
        }
    }

    [RelayCommand]
    public async Task ComputeSalesMetricsAsync()
    {
        if (SelectedCafe == null || _isBusy) return;

        try
        {
            _isBusy = true;
            IsRefreshing = true;
            await ComputeSalesMetricsInternalAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to calculate sales: {ex.Message}", "OK");
        }
        finally
        {
            _isBusy = false;
            IsRefreshing = false;
        }
    }

    private async Task ComputeSalesMetricsInternalAsync()
    {
        if (SelectedCafe == null) return;

        var allOrders = await _adminService.GetCafeOrdersAsync(SelectedCafe.Id, null);
        var today = DateTime.Today;
        var thisMonth = today.Month;
        var thisYear = today.Year;

        var validOrders = allOrders.Where(o => o.Status != "Cancelled").ToList();

        TodayRevenue = validOrders
            .Where(o => o.OrderDate.Date == today)
            .Sum(o => o.TotalAmount);

        MonthRevenue = validOrders
            .Where(o => o.OrderDate.Year == thisYear && o.OrderDate.Month == thisMonth)
            .Sum(o => o.TotalAmount);

        TotalCompletedOrders = validOrders
            .Count(o => o.Status == "Completed" || o.Status == "Delivered");

        AverageTicketValue = validOrders.Any()
            ? validOrders.Average(o => o.TotalAmount)
            : 0.00m;

        PickupOrderCount = allOrders.Count(o => o.OrderType == "Pickup");
        DeliveryOrderCount = allOrders.Count(o => o.OrderType == "Delivery");

        var topItems = allOrders
            .SelectMany(o => o.OrderItems)
            .GroupBy(oi => oi.Product != null ? oi.Product.Name : "Meal Item")
            .Select(g => new TopSellingItemDto
            {
                Name = g.Key,
                QuantitySold = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
            })
            .OrderByDescending(t => t.QuantitySold)
            .Take(5)
            .ToList();

        TopSellingItems = new ObservableCollection<TopSellingItemDto>(topItems);
        HasSalesData = validOrders.Any();

        // Load & Compute Operating Expenses
        var expenses = await _adminService.GetCafeExpensesAsync(SelectedCafe.Id);
        CafeExpenses = new ObservableCollection<Expense>(expenses);
        HasExpenses = expenses.Any();

        MonthExpenses = expenses
            .Where(e => e.ExpenseDate.Year == thisYear && e.ExpenseDate.Month == thisMonth)
            .Sum(e => e.Amount);

        NetProfit = MonthRevenue - MonthExpenses;
        ProfitMarginPercent = MonthRevenue > 0
            ? Math.Round((NetProfit / MonthRevenue) * 100, 1)
            : 0;
    }

    [RelayCommand]
    public async Task LogExpenseAsync()
    {
        if (SelectedCafe == null) return;

        string title = await Shell.Current.DisplayPromptAsync(
            "New Café Expense 📝",
            "Enter expense description (e.g. 10L Milk, Coffee Beans, Cups):",
            placeholder: "Item description",
            maxLength: 50,
            accept: "Next",
            cancel: "Cancel");

        if (string.IsNullOrWhiteSpace(title)) return;

        string amountStr = await Shell.Current.DisplayPromptAsync(
            "Expense Cost 💵",
            $"Enter amount spent for '{title}':",
            placeholder: "e.g. 25.00",
            maxLength: 8,
            keyboard: Keyboard.Numeric,
            accept: "Next",
            cancel: "Cancel");

        if (string.IsNullOrWhiteSpace(amountStr)) return;

        if (!decimal.TryParse(amountStr, System.Globalization.NumberStyles.Currency | System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out decimal amount) &&
            !decimal.TryParse(amountStr, out amount))
        {
            await Shell.Current.DisplayAlertAsync("Invalid Amount", "Please enter a valid numeric cost.", "OK");
            return;
        }

        if (amount <= 0)
        {
            await Shell.Current.DisplayAlertAsync("Invalid Amount", "Expense amount must be greater than $0.00.", "OK");
            return;
        }

        string category = await Shell.Current.DisplayActionSheet(
            "Select Expense Category 🏷️",
            "Cancel",
            null,
            "Ingredients", "Supplies", "Utilities", "Other");

        if (string.IsNullOrWhiteSpace(category) || category == "Cancel")
        {
            category = "Ingredients";
        }

        var expense = new Expense
        {
            CafeId = SelectedCafe.Id,
            Title = title.Trim(),
            Category = category,
            Amount = amount,
            ExpenseDate = DateTime.Now
        };

        bool saved = await _adminService.AddExpenseAsync(expense);
        if (saved)
        {
            CafeExpenses.Insert(0, expense);
            HasExpenses = true;

            var today = DateTime.Today;
            MonthExpenses = CafeExpenses
                .Where(e => e.ExpenseDate.Year == today.Year && e.ExpenseDate.Month == today.Month)
                .Sum(e => e.Amount);

            NetProfit = MonthRevenue - MonthExpenses;
            ProfitMarginPercent = MonthRevenue > 0
                ? Math.Round((NetProfit / MonthRevenue) * 100, 1)
                : 0;

            await Shell.Current.DisplayAlertAsync("Expense Saved 💵", $"Logged ${amount:F2} for '{title}'.", "OK");
        }
        else
        {
            await Shell.Current.DisplayAlertAsync("Error", "Failed to save expense in database.", "OK");
        }
    }

    [RelayCommand]
    public async Task DeleteExpenseAsync(Expense expense)
    {
        if (expense == null) return;

        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Delete Expense 🗑️",
            $"Remove expense '{expense.Title}' (${expense.Amount:F2})?",
            "Delete",
            "Cancel");

        if (!confirm) return;

        bool deleted = await _adminService.DeleteExpenseAsync(expense.Id);
        if (deleted)
        {
            CafeExpenses.Remove(expense);
            HasExpenses = CafeExpenses.Any();

            var today = DateTime.Today;
            MonthExpenses = CafeExpenses
                .Where(e => e.ExpenseDate.Year == today.Year && e.ExpenseDate.Month == today.Month)
                .Sum(e => e.Amount);

            NetProfit = MonthRevenue - MonthExpenses;
            ProfitMarginPercent = MonthRevenue > 0
                ? Math.Round((NetProfit / MonthRevenue) * 100, 1)
                : 0;
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

    private void ReplaceProductInCollection(Product product)
    {
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
                IsAvailable = product.IsAvailable,
                ImageUrl = product.ImageUrl,
                StockQuantity = product.StockQuantity,
                LowStockThreshold = product.LowStockThreshold,
                Cafe = product.Cafe
            };
        }
    }

    [RelayCommand]
    public async Task AddProductAsync()
    {
        if (SelectedCafe == null) return;

        // Step 1: Item Name
        string name = await Shell.Current.DisplayPromptAsync(
            "New Menu Item 🍔",
            "Enter dish or drink name:",
            placeholder: "e.g. Vanilla Iced Latte",
            maxLength: 50,
            accept: "Next",
            cancel: "Cancel");

        if (string.IsNullOrWhiteSpace(name)) return;

        // Step 2: Category
        string category = await Shell.Current.DisplayActionSheet(
            "Select Menu Category 🏷️",
            "Cancel",
            null,
            "Beverages", "Meals", "Pastries", "Snacks", "Combos", "Desserts");

        if (string.IsNullOrWhiteSpace(category) || category == "Cancel")
        {
            category = "Beverages";
        }

        // Step 3: Price
        string priceStr = await Shell.Current.DisplayPromptAsync(
            "Item Price 🏷️",
            $"Enter price for '{name}':",
            placeholder: "e.g. 4.50",
            maxLength: 8,
            keyboard: Keyboard.Numeric,
            accept: "Next",
            cancel: "Cancel");

        if (string.IsNullOrWhiteSpace(priceStr)) return;

        if (!decimal.TryParse(priceStr, System.Globalization.NumberStyles.Currency | System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out decimal price) &&
            !decimal.TryParse(priceStr, out price))
        {
            await Shell.Current.DisplayAlertAsync("Invalid Price", "Please enter a valid numeric price.", "OK");
            return;
        }

        if (price <= 0)
        {
            await Shell.Current.DisplayAlertAsync("Invalid Price", "Price must be greater than $0.00.", "OK");
            return;
        }

        // Step 4: Optional Initial Stock Count
        string stockStr = await Shell.Current.DisplayPromptAsync(
            "Initial Stock (Optional) 📦",
            "Enter initial quantity available:\n(Leave empty for untracked simple in-stock mode)",
            placeholder: "e.g. 25 (or leave empty)",
            maxLength: 5,
            keyboard: Keyboard.Numeric,
            accept: "Create",
            cancel: "Skip / Untracked");

        int? stockQty = null;
        if (!string.IsNullOrWhiteSpace(stockStr) && int.TryParse(stockStr, out int parsedStock) && parsedStock >= 0)
        {
            stockQty = parsedStock;
        }

        var newProduct = new Product
        {
            CafeId = SelectedCafe.Id,
            Name = name.Trim(),
            Category = category,
            Price = price,
            Description = $"Freshly prepared {name.Trim()} from {SelectedCafe.Name}.",
            IsAvailable = true,
            StockQuantity = stockQty,
            LowStockThreshold = 5,
            ImageUrl = "dotnet_bot.png"
        };

        bool created = await _adminService.AddProductAsync(newProduct);
        if (created)
        {
            CafeProducts.Insert(0, newProduct);
            IsStockEmpty = false;
            await Shell.Current.DisplayAlertAsync("Item Created 🎉", $"'{newProduct.Name}' has been added to your café menu!", "OK");
        }
        else
        {
            await Shell.Current.DisplayAlertAsync("Error", "Failed to add menu item to database.", "OK");
        }
    }

    [RelayCommand]
    public async Task DeleteProductAsync(Product product)
    {
        if (product == null) return;

        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Delete Menu Item 🗑️",
            $"Are you sure you want to permanently remove '{product.Name}' (${product.Price:F2}) from your menu?",
            "Yes, Delete",
            "Cancel");

        if (!confirm) return;

        bool deleted = await _adminService.DeleteProductAsync(product.Id);
        if (deleted)
        {
            CafeProducts.Remove(product);
            IsStockEmpty = CafeProducts.Count == 0;
            await Shell.Current.DisplayAlertAsync("Item Removed 🗑️", $"'{product.Name}' was removed from the menu.", "OK");
        }
        else
        {
            await Shell.Current.DisplayAlertAsync("Error", "Failed to delete item from database.", "OK");
        }
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
            ReplaceProductInCollection(product);

            string badge = targetStatus ? "IN STOCK ✅" : "SOLD OUT ❌";
            await Shell.Current.DisplayAlertAsync("Inventory Updated", $"'{product.Name}' is now marked {badge}.", "OK");
        }
        else
        {
            await Shell.Current.DisplayAlertAsync("Error", "Failed to update item availability in database.", "OK");
        }
    }

    [RelayCommand]
    public async Task IncreaseStockAsync(Product product)
    {
        if (product == null) return;

        int newQty = (product.StockQuantity ?? 10) + 1;
        bool updated = await _adminService.UpdateProductStockAsync(product.Id, newQty);
        if (updated)
        {
            product.StockQuantity = newQty;
            product.IsAvailable = true;
            ReplaceProductInCollection(product);
        }
    }

    [RelayCommand]
    public async Task DecreaseStockAsync(Product product)
    {
        if (product == null) return;

        int current = product.StockQuantity ?? 10;
        int newQty = Math.Max(0, current - 1);
        bool updated = await _adminService.UpdateProductStockAsync(product.Id, newQty);
        if (updated)
        {
            product.StockQuantity = newQty;
            if (newQty == 0) product.IsAvailable = false;
            ReplaceProductInCollection(product);
        }
    }

    [RelayCommand]
    public async Task EditStockCountAsync(Product product)
    {
        if (product == null) return;

        string currentVal = product.StockQuantity.HasValue ? product.StockQuantity.Value.ToString() : string.Empty;
        string result = await Shell.Current.DisplayPromptAsync(
            "Stock Inventory 📦",
            $"Set stock count for {product.Name}:\n(Leave empty to switch back to simple toggle mode)",
            initialValue: currentVal,
            maxLength: 5,
            keyboard: Keyboard.Numeric,
            accept: "Save",
            cancel: "Cancel");

        if (result == null) return; // Cancel pressed

        int? newStock = null;
        if (!string.IsNullOrWhiteSpace(result))
        {
            if (int.TryParse(result, out int parsed) && parsed >= 0)
            {
                newStock = parsed;
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Invalid Quantity", "Please enter a valid positive number or leave empty.", "OK");
                return;
            }
        }

        bool updated = await _adminService.UpdateProductStockAsync(product.Id, newStock);
        if (updated)
        {
            product.StockQuantity = newStock;
            if (newStock.HasValue && newStock.Value == 0) product.IsAvailable = false;
            else if (newStock.HasValue && newStock.Value > 0) product.IsAvailable = true;
            ReplaceProductInCollection(product);

            string msg = newStock.HasValue 
                ? $"Stock for '{product.Name}' set to {newStock.Value} units."
                : $"Stock tracking disabled for '{product.Name}'. Now in simple toggle mode.";
            await Shell.Current.DisplayAlertAsync("Stock Updated 📦", msg, "OK");
        }
    }

    [RelayCommand]
    public async Task EditProductPriceAsync(Product product)
    {
        if (product == null) return;

        string result = await Shell.Current.DisplayPromptAsync(
            "Update Price 🏷️",
            $"Enter new price for {product.Name}:",
            initialValue: product.Price.ToString("0.00"),
            maxLength: 8,
            keyboard: Keyboard.Numeric,
            accept: "Save",
            cancel: "Cancel");

        if (string.IsNullOrWhiteSpace(result)) return;

        if (!decimal.TryParse(result, System.Globalization.NumberStyles.Currency | System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out decimal newPrice) &&
            !decimal.TryParse(result, out newPrice))
        {
            await Shell.Current.DisplayAlertAsync("Invalid Price", "Please enter a valid numeric price (e.g. 4.50).", "OK");
            return;
        }

        if (newPrice <= 0)
        {
            await Shell.Current.DisplayAlertAsync("Invalid Price", "Price must be greater than $0.00.", "OK");
            return;
        }

        bool updated = await _adminService.UpdateProductPriceAsync(product.Id, newPrice);
        if (updated)
        {
            product.Price = newPrice;
            ReplaceProductInCollection(product);
            await Shell.Current.DisplayAlertAsync("Price Updated 🏷️", $"Price for '{product.Name}' updated to ${newPrice:F2}.", "OK");
        }
        else
        {
            await Shell.Current.DisplayAlertAsync("Error", "Failed to update price in database.", "OK");
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
        if (order == null || order.Status == "Completed" || order.Status == "Cancelled") return;
        string nextStatus = order.Status switch
        {
            "Pending" => "Preparing",
            "Preparing" => "Ready",
            "Ready" => "Completed",
            _ => "Completed"
        };
        await ExecuteStatusUpdate(order, nextStatus);
    }

    [RelayCommand]
    private async Task CancelOrderAsync(Order order)
    {
        if (order == null || order.Status == "Completed" || order.Status == "Cancelled") return;

        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Cancel Ticket ⚠️",
            $"Are you sure you want to cancel Ticket #{order.Id}?",
            "Yes, Cancel",
            "No");

        if (!confirm) return;

        await ExecuteStatusUpdate(order, "Cancelled");
    }

    private async Task ExecuteStatusUpdate(Order order, string newStatus)
    {
        if (order == null || string.IsNullOrWhiteSpace(newStatus)) return;

        bool updated = await _adminService.UpdateOrderStatusAsync(order.Id, newStatus);
        if (updated)
        {
            order.Status = newStatus;

            // Only show alert upon completion or cancellation to avoid annoying popups during fast kitchen prep
            if (newStatus == "Completed")
            {
                await Shell.Current.DisplayAlertAsync("Order Completed ✔️", $"Ticket #{order.Id} is marked Completed and ready for pickup/delivery.", "OK");
            }
            else if (newStatus == "Cancelled")
            {
                await Shell.Current.DisplayAlertAsync("Order Cancelled ✕", $"Ticket #{order.Id} has been cancelled.", "OK");
            }

            // If a filter like "Pending" is active, smoothly remove from list without rebuilding entire collection
            if (SelectedStatusFilter != "All" && newStatus != SelectedStatusFilter)
            {
                IncomingOrders.Remove(order);
                IsEmpty = IncomingOrders.Count == 0;
            }

            // Deliberately do NOT call LoadOrdersInternalAsync() here so scroll position remains intact!
        }
        else
        {
            await Shell.Current.DisplayAlertAsync("Error", "Failed to update order status in database.", "OK");
        }
    }
}
