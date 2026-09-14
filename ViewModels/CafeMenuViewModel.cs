using System.Collections.ObjectModel;
using CampusBite.Models;
using CampusBite.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CampusBite.ViewModels;

[QueryProperty(nameof(Cafe), "Cafe")]
public partial class CafeMenuViewModel : ObservableObject
{
    private readonly IProductService _productService;

    [ObservableProperty]
    private Cafe? _cafe;

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

        // Stub ready for FoodDetailsPage in Version 1.3.2!
        await Shell.Current.DisplayAlertAsync("Item Selected", $"You selected '{product.Name}' (${product.Price:F2}). Details page coming next!", "OK");
    }
}
