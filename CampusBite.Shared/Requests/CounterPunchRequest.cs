using System.ComponentModel.DataAnnotations;

namespace CampusBite.Shared.Requests;

public class CounterPunchRequest
{
    [Required(ErrorMessage = "Student username is required.")]
    public string Username { get; set; } = string.Empty;

    public int? ProductId { get; set; }

    public string? Notes { get; set; }
}
