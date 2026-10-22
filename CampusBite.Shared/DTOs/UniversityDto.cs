namespace CampusBite.Shared.DTOs;

public class UniversityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CampusName { get; set; }
    public string City { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
