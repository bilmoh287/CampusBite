using CampusBite.ViewModels;

namespace CampusBite.Views;

public partial class CafeMenuPage : ContentPage
{
    public CafeMenuPage(CafeMenuViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
