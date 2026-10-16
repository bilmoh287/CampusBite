namespace CampusBite.DTOs;

public class TopSellingItemDto
{
    public string Name { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}
