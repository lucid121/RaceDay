using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

// Table Enrolment
// One row = one Participant entering one Category.
// A user can only enter the same category once (unique rule set in DbContext).
public class Enrolment
{
    // Primary key
    public int EnrolmentId { get; set; }

    // Foreign key -> User (the Participant)
    public int UserId { get; set; }

    // Foreign key -> Category (the option they picked)
    public int CategoryId { get; set; }

    // Set automatically by the database when the row is created
    public DateTime EnrolmentDate { get; set; }

    // Only "Pending", "Confirmed" or "Completed" is allowed. Default is "Pending".
    [Required, MaxLength(50)]
    public string Status { get; set; } = "Pending";

    // Navigation: the Participant
    public User User { get; set; } = null!;

    // Navigation: the chosen Category (and through it, the Event)
    public Category Category { get; set; } = null!;

    // Navigation: the result for this enrolment (can be null until the race is done)
    public Result? Result { get; set; }
}