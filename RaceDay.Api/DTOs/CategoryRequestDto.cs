using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

// The data an Organiser sends to CREATE or UPDATE a category
// Example: "10km Run", 10 km, R150
public class CategoryRequestDto
{
    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    // In km
    [Range(0.01, 999.99, ErrorMessage = "Distance must be between 0.01 and 999.99 km.")]
    public decimal Distance { get; set; }

    // Price in Rand. 0 means free.
    [Range(0, 100000, ErrorMessage = "Entry fee must be between 0 and 100000.")]
    public decimal EntryFee { get; set; }
}