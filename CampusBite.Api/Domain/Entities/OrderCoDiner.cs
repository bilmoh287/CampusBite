namespace CampusBite.Api.Domain.Entities;

public class OrderCoDiner
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int StudentId { get; set; }
    public int? StudentContractId { get; set; }
    public int CreditsDeducted { get; set; } = 1;
    public decimal UpgradeCashPortion { get; set; } = 0.00m;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Order Order { get; set; } = null!;
    public User Student { get; set; } = null!;
    public StudentContract? StudentContract { get; set; }
}
