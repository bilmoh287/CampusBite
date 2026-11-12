using System.ComponentModel.DataAnnotations;

namespace CampusBite.Shared.Requests;

public class SubscribeToPlanRequest
{
    [Required]
    public int ContractPlanId { get; set; }
}

