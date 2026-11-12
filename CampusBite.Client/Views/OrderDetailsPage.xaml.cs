using CampusBite.ViewModels;

namespace CampusBite.Views;

public partial class OrderDetailsPage : ContentPage
{
    private readonly OrderDetailsViewModel _viewModel;

    public OrderDetailsPage(OrderDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshOrderCommand.ExecuteAsync(null);
    }
}
