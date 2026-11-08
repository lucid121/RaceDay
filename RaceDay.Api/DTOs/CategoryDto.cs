namespace RaceDay.Api.DTOs;

// The category data we send BACK to the user
public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public decimal EntryFee { get; set; }
}
