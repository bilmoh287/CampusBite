namespace CampusBite.Api.Domain.Entities;

public class ContractPlan
{
    public int Id { get; set; }
    public int CafeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int TotalCredits { get; set; }
    public int ValidityDays { get; set; } = 30;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Cafe Cafe { get; set; } = null!;
    public ICollection<StudentContract> StudentContracts { get; set; } = new List<StudentContract>();
}
