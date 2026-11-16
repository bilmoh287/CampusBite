namespace CampusBite.Shared.DTOs;

public class UserSearchResultDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool HasActivePassAtCafe { get; set; }
    public int RemainingPassCredits { get; set; }
}
