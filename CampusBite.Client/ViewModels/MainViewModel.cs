using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CampusBite.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private int _count;

    [ObservableProperty]
    private string _counterText = "Click me";

    [RelayCommand]
    private void IncrementCount()
    {
        Count++;
        if (Count == 1)
            CounterText = $"Clicked {Count} time";
        else
            CounterText = $"Clicked {Count} times";
    }
}
