using CampusBite.Views;

namespace CampusBite
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(CafeMenuPage), typeof(CafeMenuPage));
        }
    }
}
