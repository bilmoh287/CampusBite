using System.ComponentModel.DataAnnotations;

namespace CampusBite.Shared.Requests;

public class CreateContractPlanRequest
{
    [Required]
    public int CafeId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(1, 100000, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    [Range(1, 365, ErrorMessage = "Total credits must be between 1 and 365.")]
    public int TotalCredits { get; set; }

    [Range(1, 365, ErrorMessage = "Validity days must be between 1 and 365.")]
    public int ValidityDays { get; set; } = 30;
}

