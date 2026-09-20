using CampusBite.ViewModels;

namespace CampusBite.Views;

public partial class FoodDetailsPage : ContentPage
{
    public FoodDetailsPage(FoodDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
