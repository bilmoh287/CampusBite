using System.ComponentModel.DataAnnotations;

namespace CampusBite.Shared.Requests;

public class UpdatePackagingFeeRequest
{
    [Range(0, 100, ErrorMessage = "Packaging fee must be between 0 and 100 ETB.")]
    public decimal TakeawayPackagingFee { get; set; }
}
