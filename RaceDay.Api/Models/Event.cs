using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

// Table Event
// One row = one race day, for example "Soweto Marathon".
public class Event
{
    // Primary key
    public int EventId { get; set; }

    // Foreign key -> User table. The Organiser who created this event.
    public int OrganiserId { get; set; }

    [Required, MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    // DateOnly = a date with no time (matches SQL "Date")
    public DateOnly EventDate { get; set; }

    [Required, MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    // Total distance in km. Example: 42.20 (5 digits in total, 2 after the dot)
    [Column(TypeName = "decimal(5,2)")]
    public decimal Distance { get; set; }

    // Only "Run", "Walk" or "Cycle" is allowed (checked in the database)
    [Required, MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    // Optional text about the event (the "?" means it can be empty / null)
    [MaxLength(1000)]
    public string? Description { get; set; }

    // Navigation: the Organiser who owns this event
    public User Organiser { get; set; } = null!;

    // Navigation: the categories inside this event (one event -> many categories)
    public List<Category> Categories { get; set; } = new();
}