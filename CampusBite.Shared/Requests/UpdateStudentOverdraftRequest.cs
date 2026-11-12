using System.ComponentModel.DataAnnotations;

namespace CampusBite.Shared.Requests;

public class UpdateStudentOverdraftRequest
{
    public bool AllowOverdraft { get; set; }

    [Range(0, 50, ErrorMessage = "Overdraft limit must be between 0 and 50 meals.")]
    public int OverdraftLimit { get; set; }
}

