namespace CampusBite.Shared.DTOs;

public class CoDinerDto
{
    public int StudentId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int CreditsDeducted { get; set; } = 1;
    public decimal UpgradeCashPortion { get; set; }
}
