using System.ComponentModel.DataAnnotations;

namespace CampusBite.Shared.Requests;

public class UpdateProductRequest
{
    [Required(ErrorMessage = "Category ID is required.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [Range(0.01, 100000, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    [Range(0, 100000, ErrorMessage = "Stock quantity cannot be negative.")]
    public int? StockQuantity { get; set; }

    public bool IsAvailable { get; set; }
}
