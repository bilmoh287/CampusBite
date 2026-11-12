using System.Text.Json.Serialization;

namespace CampusBite.Shared.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContractStatus
{
    Active = 0,
    Depleted = 1,
    Expired = 2,
    Suspended = 3
}
