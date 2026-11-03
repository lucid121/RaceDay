using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

// Table Result
// One row = the race result for one enrolment (one-to-one).
// The Organiser fills this in after the event.
public class Result
{
    // Primary key
    public int ResultId { get; set; }

    // Foreign key -> Enrolment. Must be unique so one enrolment has only one result.
    public int EnrolmentId { get; set; }

    // Time taken to finish, for example 01:45:22. TimeSpan matches SQL "Time".
    // Null means "no time captured yet".
    public TimeSpan? FinishTime { get; set; }

    // Finishing place, for example 12. Null until captured.
    public int? Position { get; set; }

    // Optional comment from the Organiser
    [MaxLength(255)]
    public string? Remarks { get; set; }

    // Navigation: the enrolment this result belongs to
    public Enrolment Enrolment { get; set; } = null!;
} 

