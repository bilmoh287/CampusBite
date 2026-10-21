namespace CampusBite.ViewModels;

[QueryProperty(nameof(Order), "Order")]
public partial class OrderDetailsViewModel : ObservableObject
{
    [ObservableProperty]
    private Order? _order;

    public int ItemCount => Order?.OrderItems?.Sum(i => i.Quantity) ?? 0;
    public string FormattedDate => Order?.OrderDate.ToString("f") ?? string.Empty;
}
