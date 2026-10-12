using System.Collections.ObjectModel;
using CampusBite.Models;
using CampusBite.Services.Interfaces;
using CampusBite.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CampusBite.ViewModels;

[QueryProperty(nameof(Cafe), "Cafe")]
public partial class CafeMenuViewModel : ObservableObject
{
    private readonly IProductService _productService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCafeClosed))]
    private Cafe? _cafe;

    public bool IsCafeClosed => Cafe != null && !Cafe.IsOpen;

    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private bool _isBusy;

    public CafeMenuViewModel(IProductService productService)
    {
        _productService = productService;
    }

    partial void OnCafeChanged(Cafe? value)
    {
        if (value != null)
        {
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

            // Load Products for current selected category
            var productList = await _productService.GetProductsByCafeIdAsync(Cafe.Id, SelectedCategory);
            Products.Clear();
            foreach (var product in productList)
            {
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
}
