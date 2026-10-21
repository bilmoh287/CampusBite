using System.Collections.ObjectModel;
using CampusBite.Models;
using CampusBite.Services.Interfaces;
using CampusBite.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CampusBite.ViewModels;

public partial class CafeListViewModel : ObservableObject
{
    private readonly ICafeService _cafeService;

    public ObservableCollection<Cafe> Cafes { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = "Campus Cafés";

    public CafeListViewModel(ICafeService cafeService)
    {
        _cafeService = cafeService;
    }

    private bool _isProcessing;

    [RelayCommand]
    public async Task LoadCafesAsync()
    {
        if (_isProcessing) return;

        try
        {
            _isProcessing = true;
            IsBusy = true;
            var cafeList = await _cafeService.GetCafesAsync();

            Cafes.Clear();
            foreach (var cafe in cafeList)
            {
                Cafes.Add(cafe);
            }
        }
        finally
        {
            _isProcessing = false;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SelectCafeAsync(Cafe cafe)
    {
        if (cafe == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "Cafe", cafe }
        };

        await Shell.Current.GoToAsync(nameof(CafeMenuPage), navigationParameter);
    }
}
