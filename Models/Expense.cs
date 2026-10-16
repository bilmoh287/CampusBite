namespace CampusBite.Models;

public class Expense
{
    public int Id { get; set; }
    public int CafeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Ingredients";
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.Now;

    // Navigation property
    public Cafe? Cafe { get; set; }
}
