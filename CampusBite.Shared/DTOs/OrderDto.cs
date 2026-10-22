using CampusBite.Shared.Enums;

namespace CampusBite.Shared.DTOs;

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int CafeId { get; set; }
    public string CafeName { get; set; } = string.Empty;
    public int? CourierId { get; set; }
    public string? CourierName { get; set; }
    public OrderStatus Status { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string DeliveryLocation { get; set; } = string.Empty;
    public string? SpecialNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<OrderItemDto> Items { get; set; } = new();
}
