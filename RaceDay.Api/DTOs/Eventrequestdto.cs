using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

// The data an Organiser sends to CREATE or UPDATE an event.
public class EventRequestDto
{
    [Required, MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    // Date only, written like 2027-03-14
    [Required]
    public DateOnly EventDate { get; set; }

    [Required, MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    // In km. The database allows up to 999.99
    [Range(0.01, 999.99, ErrorMessage = "Distance must be between 0.01 and 999.99 km.")]
    public decimal Distance { get; set; }

    // Only these three words are allowed
    [Required]
    [RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "EventType must be Run, Walk or Cycle.")]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }
}