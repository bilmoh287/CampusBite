namespace CampusBite.Models;

public partial class Order : ObservableObject
{
    public int Id { get; set; }
    public int CafeId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTerminalStatus))]
    [NotifyPropertyChangedFor(nameof(CanBeCancelled))]
    private string _status = "Pending";

    public string OrderType { get; set; } = "Pickup";
    public string? DeliveryLocation { get; set; }
    public string? Notes { get; set; }

    // Helper properties for XAML bindings
    public bool HasDeliveryLocation => !string.IsNullOrWhiteSpace(DeliveryLocation);
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
    public bool IsDelivery => OrderType == "Delivery";

    // Helper properties for ticket action buttons UX
    public bool IsTerminalStatus => Status == "Completed" || Status == "Cancelled";
    public bool CanBeCancelled => !IsTerminalStatus;

    // Navigation properties
    public Cafe? Cafe { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
