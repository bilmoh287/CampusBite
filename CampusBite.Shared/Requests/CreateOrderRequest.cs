using CampusBite.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace CampusBite.Shared.Requests;

public class CreateOrderRequest
{
    [Required]
    public int CafeId { get; set; }

    [StringLength(200, ErrorMessage = "Delivery location cannot exceed 200 characters.")]
    public string? DeliveryLocation { get; set; }

    public string? OrderType { get; set; }

    public PackagingOption PackagingOption { get; set; } = PackagingOption.StandardFree;

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    public string? SpecialNotes { get; set; }

    public List<string> CoDinerUsernames { get; set; } = new();

    [Required]
    [MinLength(1, ErrorMessage = "Order must contain at least one item.")]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}
