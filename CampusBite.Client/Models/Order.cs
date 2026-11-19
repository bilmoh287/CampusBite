using CampusBite.Shared.Enums;

namespace CampusBite.Models;

public partial class Order : ObservableObject
{
    public int Id { get; set; }
    public int CafeId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTerminalStatus))]
    [NotifyPropertyChangedFor(nameof(CanBeCancelled))]
    private string _status = "Pending";

    public string OrderType { get; set; } = "DineIn";
    public PackagingOption PackagingOption { get; set; } = PackagingOption.StandardFree;
    public decimal PackagingFee { get; set; } = 0.00m;
    public string? DeliveryLocation { get; set; }
    public string? Notes { get; set; }
    public string? CourierName { get; set; }

    // Helper properties for XAML bindings
    public bool HasDeliveryLocation => !string.IsNullOrWhiteSpace(DeliveryLocation);
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
    public bool IsDelivery => string.Equals(OrderType, "Delivery", StringComparison.OrdinalIgnoreCase);
    public bool IsDineIn => string.Equals(OrderType, "DineIn", StringComparison.OrdinalIgnoreCase);
    public bool IsPickup => string.Equals(OrderType, "Pickup", StringComparison.OrdinalIgnoreCase);

    public string DiningModeBadgeText => IsDineIn ? "🍽️ Dine-In" : (IsPickup ? "🚶 Pickup" : "🛵 Delivery");
    public Color DiningModeBadgeColor => IsDineIn ? Color.FromArgb("#81C784") : (IsPickup ? Color.FromArgb("#64B5F6") : Color.FromArgb("#FF9800"));
    public Color DiningModeBadgeBgColor => IsDineIn ? Color.FromArgb("#142E1B") : (IsPickup ? Color.FromArgb("#10253B") : Color.FromArgb("#2E2010"));

    public string PackagingBadgeText
    {
        get
        {
            if (IsDineIn) return "🍽️ Ceramic Plate";
            return PackagingOption switch
            {
                PackagingOption.TakeawayBox => PackagingFee > 0 ? $"📦 Sturdy Box (+{PackagingFee:F0} ETB)" : "📦 Sturdy Box",
                PackagingOption.OwnContainer => "🥣 Own Container",
                _ => "🛍️ Festal (Free)"
            };
        }
    }

    public Color PackagingBadgeColor
    {
        get
        {
            if (IsDineIn) return Color.FromArgb("#81C784");
            return PackagingOption switch
            {
                PackagingOption.TakeawayBox => Color.FromArgb("#FFB74D"),
                PackagingOption.OwnContainer => Color.FromArgb("#BA68C8"),
                _ => Color.FromArgb("#81D4FA")
            };
        }
    }

    public Color PackagingBadgeBgColor
    {
        get
        {
            if (IsDineIn) return Color.FromArgb("#142E1B");
            return PackagingOption switch
            {
                PackagingOption.TakeawayBox => Color.FromArgb("#2E2010"),
                PackagingOption.OwnContainer => Color.FromArgb("#281630"),
                _ => Color.FromArgb("#0E2536")
            };
        }
    }

    // Helper properties for ticket action buttons UX
    public bool IsTerminalStatus => Status == "Completed" || Status == "Cancelled" || Status == "Delivered";
    public bool CanBeCancelled => !IsTerminalStatus;

    // Navigation properties
    public Cafe? Cafe { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
