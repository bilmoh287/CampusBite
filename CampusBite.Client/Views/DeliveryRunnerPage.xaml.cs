namespace CampusBite.Views;

public partial class DeliveryRunnerPage : ContentPage
{
    private readonly DeliveryRunnerViewModel _viewModel;

    public DeliveryRunnerPage(DeliveryRunnerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDeliveriesCommand.ExecuteAsync(null);
    }
}
