using System.Text.Json.Serialization;

namespace CampusBite.Shared.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PackagingOption
{
    StandardFree = 0,   // Plate (Dine-In), Festal / Paper wrap (Pickup/Delivery) - 0 ETB
    TakeawayBox = 1,    // Sturdy disposable plastic/foil container (+TakeawayPackagingFee)
    OwnContainer = 2    // Student brings own dish/lunchbox - 0 ETB
}
