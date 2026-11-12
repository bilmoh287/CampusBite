using CampusBite.Shared.Enums;

namespace CampusBite.Shared.DTOs;

public class StudentContractDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int ContractPlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int CafeId { get; set; }
    public string CafeName { get; set; } = string.Empty;
    public int TotalCredits { get; set; }
    public int RemainingCredits { get; set; }
    public int UsedCredits => TotalCredits - RemainingCredits;

    // Debt / Overdraft tab properties
    public bool AllowOverdraft { get; set; }
    public int OverdraftLimit { get; set; }
    public int TotalAvailableCredits => RemainingCredits + (AllowOverdraft ? OverdraftLimit : 0);
    public bool IsInDebt => RemainingCredits < 0;
    public int DebtMealsCount => RemainingCredits < 0 ? Math.Abs(RemainingCredits) : 0;

    public ContractStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysRemaining => Math.Max(0, (int)(EndDate.Date - DateTime.UtcNow.Date).TotalDays);
    public bool IsActive => Status == ContractStatus.Active && EndDate >= DateTime.UtcNow;
}

