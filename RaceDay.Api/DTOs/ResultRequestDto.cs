using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

// What an Organiser sends to save a result for one enrolment
public class ResultRequestDto
{
    // Time taken, written like 01:45:22 (hours:minutes:seconds)
    [Required]
    public TimeSpan? FinishTime { get; set; }

    // Finishing place: 1 = first
    [Range(1, 100000, ErrorMessage = "Position must be 1 or more.")]
    public int Position { get; set; }

    [MaxLength(255)]
    public string? Remarks { get; set; }
}