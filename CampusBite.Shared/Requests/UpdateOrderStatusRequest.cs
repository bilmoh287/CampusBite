using CampusBite.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace CampusBite.Shared.Requests;

public class UpdateOrderStatusRequest
{
    [Required]
    public OrderStatus NewStatus { get; set; }
}
