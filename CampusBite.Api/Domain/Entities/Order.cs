using CampusBite.Shared.Enums;

namespace CampusBite.Api.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int CafeId { get; set; }
    public int? CourierId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; } = 0.00m;
    public decimal DiscountAmount { get; set; } = 0.00m;
    public decimal TotalAmount { get; set; }
    public string DeliveryLocation { get; set; } = string.Empty;
    public string? SpecialNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public User Customer { get; set; } = null!;
    public Cafe Cafe { get; set; } = null!;
    public User? Courier { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<ContractUsageLog> ContractUsageLogs { get; set; } = new List<ContractUsageLog>();
}
