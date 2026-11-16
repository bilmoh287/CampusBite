namespace CampusBite.Api.Domain.Entities;

public class BuddyConnection
{
    public int Id { get; set; }
    public int RequesterId { get; set; }
    public int BuddyId { get; set; }
    public bool IsConfirmed { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public User Requester { get; set; } = null!;
    public User Buddy { get; set; } = null!;
}
