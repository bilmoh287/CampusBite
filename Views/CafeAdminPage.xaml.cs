namespace CampusBite.Views;

public partial class CafeAdminPage : ContentPage
{
    private readonly CafeAdminViewModel _viewModel;

    public CafeAdminPage(CafeAdminViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadAdminDataCommand.Execute(null);
    }
}
