namespace RaceDay.Api.DTOs
{
    public class CategoryDTOs
    {
        public int EventId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Distance { get; set; }
        public decimal EntryFee { get; set; }
    }
}
