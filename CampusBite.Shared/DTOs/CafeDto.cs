namespace CampusBite.Shared.DTOs;

public class CafeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal MinimumOrderAmount { get; set; }
    public bool IsActive { get; set; }
    public int? UniversityId { get; set; }
    public string? UniversityName { get; set; }
    public int OwnerId { get; set; }
    public string? OwnerName { get; set; }

    public List<CategoryDto> Categories { get; set; } = new();
    public List<ProductDto> Products { get; set; } = new();
}
