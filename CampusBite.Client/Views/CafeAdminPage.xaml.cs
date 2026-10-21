namespace CampusBite.Views;

public partial class CafeAdminPage : ContentPage
{
    private readonly CafeAdminViewModel _viewModel;

    public CafeAdminPage(CafeAdminViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAdminDataCommand.ExecuteAsync(null);
    }
}
