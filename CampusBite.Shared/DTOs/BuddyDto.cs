using System;

namespace CampusBite.Shared.DTOs;

public class BuddyDto
{
    public int ConnectionId { get; set; }
    public int BuddyId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
