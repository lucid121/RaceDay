namespace RaceDay.Api.DTOs;

// The result data we send BACK to the user
public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }

    public string ParticipantName { get; set; } = string.Empty;

    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string? Remarks { get; set; }

    // How many people finished in this category. Used for "47th out of 312 finishers".
    public int TotalFinishers { get; set; }
}
