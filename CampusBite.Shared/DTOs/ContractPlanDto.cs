namespace CampusBite.Shared.DTOs;

public class ContractPlanDto
{
    public int Id { get; set; }
    public int CafeId { get; set; }
    public string CafeName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int TotalCredits { get; set; }
    public decimal BaseMealCreditValue { get; set; } = 130.00m;
    public int ValidityDays { get; set; }
    public decimal PricePerMeal => TotalCredits > 0 ? Math.Round(Price / TotalCredits, 2) : 0;
    public bool IsActive { get; set; }
}

