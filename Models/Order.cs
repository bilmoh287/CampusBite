namespace CampusBite.Models;

public class Order
{
    public int Id { get; set; }
    public int CafeId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";
    public string OrderType { get; set; } = "Pickup";
    public string? DeliveryLocation { get; set; }
    public string? Notes { get; set; }

    // Helper properties for XAML bindings
    public bool HasDeliveryLocation => !string.IsNullOrWhiteSpace(DeliveryLocation);
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
    public bool IsDelivery => OrderType == "Delivery";

    // Navigation properties
    public Cafe? Cafe { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
