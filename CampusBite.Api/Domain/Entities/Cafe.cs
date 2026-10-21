namespace CampusBite.Api.Domain.Entities;

public class Cafe
{
    public int Id { get; set; }
    public int? UniversityId { get; set; }
    public int OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public decimal DeliveryFee { get; set; } = 0.00m;
    public decimal MinimumOrderAmount { get; set; } = 0.00m;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public University? University { get; set; }
    public User Owner { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<ContractPlan> ContractPlans { get; set; } = new List<ContractPlan>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
