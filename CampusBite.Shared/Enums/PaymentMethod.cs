using System.Text.Json.Serialization;

namespace CampusBite.Shared.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentMethod
{
    Cash = 0,
    Card = 1,
    MealContract = 2
}
