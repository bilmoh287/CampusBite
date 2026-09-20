using CampusBite.Views;

namespace CampusBite
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(CafeMenuPage), typeof(CafeMenuPage));
            Routing.RegisterRoute(nameof(FoodDetailsPage), typeof(FoodDetailsPage));
            Routing.RegisterRoute(nameof(CheckoutPage), typeof(CheckoutPage));
            Routing.RegisterRoute(nameof(OrderDetailsPage), typeof(OrderDetailsPage));
        }
    }
}
