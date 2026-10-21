namespace CampusBite.Models;

public class Cafe
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsOpen { get; set; } = true;
    public string ImageUrl { get; set; } = string.Empty;
    public string? Announcement { get; set; }

    // Navigation properties
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
