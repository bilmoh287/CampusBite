namespace CampusBite.Shared.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public int CafeId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int? StockQuantity { get; set; }
    public bool IsAvailable { get; set; }

    public bool IsLowStock => StockQuantity.HasValue && StockQuantity.Value > 0 && StockQuantity.Value <= 5;
    public bool IsOutOfStock => StockQuantity.HasValue && StockQuantity.Value == 0;
}
