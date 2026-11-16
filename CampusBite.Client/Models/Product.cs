namespace CampusBite.Models;

public class Product
{
    public int Id { get; set; }
    public int CafeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public string ImageUrl { get; set; } = string.Empty;

    // Optional Numeric Inventory (Nullable: null = Simple Untracked Mode, number = Tracked Mode)
    public int? StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;

    // Helper Properties for UX & Data Binding
    public bool HasTrackedStock => StockQuantity.HasValue;
    public bool IsLowStock => IsAvailable && StockQuantity.HasValue && StockQuantity.Value <= LowStockThreshold && StockQuantity.Value > 0;
    public string StockDisplay => StockQuantity.HasValue ? $"{StockQuantity.Value} left" : (IsAvailable ? "In Stock" : "Sold Out");

    // Dynamic Pass Eligibility properties for UI
    public bool HasActivePass { get; set; }
    public bool IsPassCovered { get; set; }
    public bool IsPassUpgrade { get; set; }
    public decimal UpgradeDifference { get; set; }
    public string PassBadgeText => IsPassCovered 
        ? "🎟️ Pass Covered" 
        : (IsPassUpgrade ? $"🎟️ Pass + {UpgradeDifference:F0} ETB" : string.Empty);

    // Navigation property
    public Cafe? Cafe { get; set; }
}
