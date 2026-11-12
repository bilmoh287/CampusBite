using System.Text.Json.Serialization;

namespace CampusBite.Shared.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    Student = 0,
    CafeOwner = 1,
    Courier = 2,
    Admin = 3,
    Customer = 4
}
