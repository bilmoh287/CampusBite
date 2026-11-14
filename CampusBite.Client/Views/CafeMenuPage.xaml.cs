using CampusBite.ViewModels;

namespace CampusBite.Views;

public partial class CafeMenuPage : ContentPage
{
    private readonly CafeMenuViewModel _viewModel;

    public CafeMenuPage(CafeMenuViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.StopLiveListeningAsync();
    }
}
