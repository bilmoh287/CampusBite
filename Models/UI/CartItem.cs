using CommunityToolkit.Mvvm.ComponentModel;

namespace CampusBite.Models.UI;

public partial class CartItem : ObservableObject
{
    public Product Product { get; set; } = null!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPrice))]
    private int _quantity;

    public decimal TotalPrice => (Product?.Price ?? 0m) * Quantity;
}
