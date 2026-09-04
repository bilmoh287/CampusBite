namespace CampusBite.Models;

public class Order
{
    public int Id { get; set; }
    public int CafeId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";
    public string OrderType { get; set; } = "Pickup";

    // Navigation properties
    public Cafe? Cafe { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
