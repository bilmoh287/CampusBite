using System.Collections.ObjectModel;
using CampusBite.Models;
using CampusBite.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CampusBite.ViewModels;

[QueryProperty(nameof(Cafe), "Cafe")]
public partial class CafeMenuViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly IOrderHubService _orderHubService;
    private readonly IContractService _contractService;
    private Cafe? _previousCafe;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCafeClosed))]
    [NotifyPropertyChangedFor(nameof(HasAnnouncement))]
    [NotifyPropertyChangedFor(nameof(Announcement))]
    private Cafe? _cafe;

    public bool IsCafeClosed => Cafe != null && !Cafe.IsOpen;
    public bool HasAnnouncement => !string.IsNullOrWhiteSpace(Cafe?.Announcement);
    public string Announcement => Cafe?.Announcement ?? string.Empty;

    [ObservableProperty]
    private StudentContractDto? _activeContract;

    [ObservableProperty]
    private bool _hasActivePass;

    [ObservableProperty]
    private string _passBannerText = string.Empty;

    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private bool _isBusy;

    public CafeMenuViewModel(
        IProductService productService, 
        IOrderHubService orderHubService,
        IContractService contractService)
    {
        _productService = productService;
        _orderHubService = orderHubService;
        _contractService = contractService;
        _orderHubService.AnnouncementUpdated += OnLiveAnnouncementUpdated;
        _orderHubService.CafeStatusChanged += OnLiveCafeStatusChanged;
    }

    private void OnLiveAnnouncementUpdated(int cafeId, string? announcement)
    {
        if (Cafe == null || Cafe.Id != cafeId) return;

        Cafe.Announcement = announcement;
        OnPropertyChanged(nameof(Cafe));
        OnPropertyChanged(nameof(HasAnnouncement));
        OnPropertyChanged(nameof(Announcement));
    }

    private void OnLiveCafeStatusChanged(int cafeId, bool isOpen)
    {
        if (Cafe == null || Cafe.Id != cafeId) return;

        Cafe.IsOpen = isOpen;
        OnPropertyChanged(nameof(Cafe));
        OnPropertyChanged(nameof(IsCafeClosed));
    }

    public async Task StopLiveListeningAsync()
    {
        if (Cafe != null)
        {
            await _orderHubService.LeaveCafeGroupAsync(Cafe.Id);
        }
    }

    partial void OnCafeChanged(Cafe? value)
    {
        if (_previousCafe != null && _previousCafe.Id != value?.Id)
        {
            _ = _orderHubService.LeaveCafeGroupAsync(_previousCafe.Id);
        }

        if (value != null)
        {
            _previousCafe = value;
            _ = _orderHubService.JoinCafeGroupAsync(value.Id);
            _ = LoadMenuAsync();
        }
    }

    [RelayCommand]
    public async Task LoadMenuAsync()
    {
        if (Cafe == null || IsBusy) return;

        try
        {
            IsBusy = true;

            // Load Categories if empty
            if (Categories.Count == 0)
            {
                var categoryList = await _productService.GetCategoriesByCafeIdAsync(Cafe.Id);
                Categories.Clear();
                foreach (var cat in categoryList)
                {
                    Categories.Add(cat);
                }
            }

            // Check for student active subscription at this café
            try
            {
                ActiveContract = await _contractService.GetActiveContractForCafeAsync(Cafe.Id);
                HasActivePass = ActiveContract != null && ActiveContract.RemainingCredits > 0 && ActiveContract.IsActive;
                if (HasActivePass && ActiveContract != null)
                {
                    PassBannerText = $"🎟️ Active Pass: {ActiveContract.RemainingCredits} meals left (Base: {ActiveContract.BaseMealCreditValue:F0} ETB)";
                }
                else
                {
                    PassBannerText = string.Empty;
                }
            }
            catch
            {
                HasActivePass = false;
                PassBannerText = string.Empty;
            }

            // Load Products for current selected category
            var productList = await _productService.GetProductsByCafeIdAsync(Cafe.Id, SelectedCategory);
            Products.Clear();
            foreach (var product in productList)
            {
                if (HasActivePass && ActiveContract != null)
                {
                    product.HasActivePass = true;
                    if (product.Price <= ActiveContract.BaseMealCreditValue)
                    {
                        product.IsPassCovered = true;
                        product.IsPassUpgrade = false;
                        product.UpgradeDifference = 0;
                    }
                    else
                    {
                        product.IsPassCovered = false;
                        product.IsPassUpgrade = true;
                        product.UpgradeDifference = product.Price - ActiveContract.BaseMealCreditValue;
                    }
                }
                else
                {
                    product.HasActivePass = false;
                    product.IsPassCovered = false;
                    product.IsPassUpgrade = false;
                    product.UpgradeDifference = 0;
                }

                Products.Add(product);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task FilterByCategoryAsync(string category)
    {
        if (string.IsNullOrWhiteSpace(category) || SelectedCategory == category) return;

        SelectedCategory = category;
        await LoadMenuAsync();
    }

    [RelayCommand]
    private async Task SelectProductAsync(Product product)
    {
        if (product == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "Product", product }
        };

        await Shell.Current.GoToAsync(nameof(FoodDetailsPage), navigationParameter);
    }

    [RelayCommand]
    private async Task NavigateToMealPlansAsync()
    {
        if (Cafe == null) return;

        await Shell.Current.GoToAsync($"{nameof(MealPlansPage)}?CafeId={Cafe.Id}");
    }
}
