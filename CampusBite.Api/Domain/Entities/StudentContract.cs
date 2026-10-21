using CampusBite.Shared.Enums;

namespace CampusBite.Api.Domain.Entities;

public class StudentContract
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int ContractPlanId { get; set; }
    public int TotalCredits { get; set; }
    public int RemainingCredits { get; set; }
    
    // Overdraft tab support for trusted/familiar customers
    public bool AllowOverdraft { get; set; } = false;
    public int OverdraftLimit { get; set; } = 0;

    public ContractStatus Status { get; set; } = ContractStatus.Active;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public User Student { get; set; } = null!;
    public ContractPlan ContractPlan { get; set; } = null!;
    public ICollection<ContractUsageLog> UsageLogs { get; set; } = new List<ContractUsageLog>();
}
