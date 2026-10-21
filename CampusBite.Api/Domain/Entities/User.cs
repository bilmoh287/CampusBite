using CampusBite.Shared.Enums;

namespace CampusBite.Api.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public int? UniversityId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Student;
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public University? University { get; set; }
    public ICollection<Cafe> OwnedCafes { get; set; } = new List<Cafe>();
    public ICollection<Order> CustomerOrders { get; set; } = new List<Order>();
    public ICollection<Order> CourierOrders { get; set; } = new List<Order>();
    public ICollection<StudentContract> StudentContracts { get; set; } = new List<StudentContract>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
