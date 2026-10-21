using CampusBite.ViewModels;

namespace CampusBite.Views;

public partial class CafeListPage : ContentPage
{
    private readonly CafeListViewModel _viewModel;

    public CafeListPage(CafeListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCafesCommand.ExecuteAsync(null);
    }
}
