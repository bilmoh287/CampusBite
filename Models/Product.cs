namespace CampusBite.Models;

public class Product
{
    public int Id { get; set; }
    public int CafeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public string ImageUrl { get; set; } = string.Empty;

    // Navigation property
    public Cafe? Cafe { get; set; }
}
