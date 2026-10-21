using CampusBite.Shared.Enums;

namespace CampusBite.Shared.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? PhoneNumber { get; set; }
    public int? UniversityId { get; set; }
    public string? UniversityName { get; set; }
    public DateTime CreatedAt { get; set; }
}
