namespace CampusBite.Shared.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public int CafeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
