using CampusBite.Shared.Enums;

namespace CampusBite.Api.Domain.Entities;

public class Expense
{
    public int Id { get; set; }
    public int CafeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public ExpenseCategory Category { get; set; } = ExpenseCategory.Other;
    public decimal Amount { get; set; }
    public DateOnly ExpenseDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Cafe Cafe { get; set; } = null!;
}
