using CampusBite.ViewModels;

namespace CampusBite.Views;

public partial class MealPlansPage : ContentPage
{
    private readonly MealPlansViewModel _viewModel;

    public MealPlansPage(MealPlansViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();
    }
}
