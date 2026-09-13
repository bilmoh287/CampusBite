using System.Collections.ObjectModel;
using CampusBite.Models;
using CampusBite.Services.Interfaces;
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

    [RelayCommand]
    public async Task LoadCafesAsync()
    {
        if (IsBusy) return;

        try
        {
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
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SelectCafeAsync(Cafe cafe)
    {
        if (cafe == null) return;

        // Navigation to CafeMenuPage will be wired up in Version 1.3!
        await Shell.Current.DisplayAlert("Café Selected", $"You selected '{cafe.Name}'. Menu page coming in Version 1.3!", "OK");
    }
}
