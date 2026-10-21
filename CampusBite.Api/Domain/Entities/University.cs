namespace CampusBite.Api.Domain.Entities;

public class University
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CampusName { get; set; }
    public string City { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Cafe> Cafes { get; set; } = new List<Cafe>();
}
