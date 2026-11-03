using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

// Table Category
// One row = one option inside an event, for example "10km Run" or "Half Marathon".
public class Category
{
    // Primary key
    public int CategoryId { get; set; }

    // Foreign key -> Event table
    public int EventId { get; set; }

    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    // Distance of this category in km
    [Column(TypeName = "decimal(5,2)")]
    public decimal Distance { get; set; }

    // Price to enter. Defaults to 0.00 in the database.
    [Column(TypeName = "decimal(10,2)")]
    public decimal EntryFee { get; set; }

    // Navigation: the event this category belongs to
    public Event Event { get; set; } = null!;

    // Navigation: people who entered this category
    public List<Enrolment> Enrolments { get; set; } = new();
}