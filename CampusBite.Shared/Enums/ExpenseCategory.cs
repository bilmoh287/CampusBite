using System.Text.Json.Serialization;

namespace CampusBite.Shared.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExpenseCategory
{
    Inventory = 0,
    Utilities = 1,
    Salaries = 2,
    Rent = 3,
    Maintenance = 4,
    Other = 5
}
