using CampusBite.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace CampusBite.Shared.Requests;

public class CreateOrderRequest
{
    [Required]
    public int CafeId { get; set; }

    [Required(ErrorMessage = "Delivery location is required.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Delivery location must be between 3 and 200 characters.")]
    public string DeliveryLocation { get; set; } = string.Empty;

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    public string? SpecialNotes { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Order must contain at least one item.")]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}
