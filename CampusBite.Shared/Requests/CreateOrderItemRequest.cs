using System.ComponentModel.DataAnnotations;

namespace CampusBite.Shared.Requests;

public class CreateOrderItemRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100.")]
    public int Quantity { get; set; } = 1;
}
