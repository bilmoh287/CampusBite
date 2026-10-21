namespace CampusBite.Api.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public int CafeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 0;

    // Navigation Properties
    public Cafe Cafe { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
