namespace CampusBite.Api.Domain.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int? ProductId { get; set; }
    
    // Historical Snapshot (Frozen at checkout)
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    
    public int Quantity { get; set; } = 1;
    public decimal ItemTotal { get; set; }

    // Navigation Properties
    public Order Order { get; set; } = null!;
    public Product? Product { get; set; }
}
