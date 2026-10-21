namespace CampusBite.Api.Domain.Entities;

public class ContractUsageLog
{
    public int Id { get; set; }
    public int StudentContractId { get; set; }
    public int OrderId { get; set; }
    public int CreditsDeducted { get; set; } = 1;
    public int RemainingCreditsAfter { get; set; }
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public StudentContract StudentContract { get; set; } = null!;
    public Order Order { get; set; } = null!;
}
