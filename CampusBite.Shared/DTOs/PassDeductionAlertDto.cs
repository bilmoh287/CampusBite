namespace CampusBite.Shared.DTOs;

public class PassDeductionAlertDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public string CafeName { get; set; } = string.Empty;
    public string? HostName { get; set; }
    public string? MealName { get; set; }
    public decimal UpgradeCashPortion { get; set; }
    public int RemainingCredits { get; set; }
}
