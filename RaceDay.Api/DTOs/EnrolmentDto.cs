namespace RaceDay.Api.DTOs;

// The enrolment data we send BACK to the user
public class EnrolmentDto
{
    public int EnrolmentId { get; set; }

    // Who entered
    public int UserId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;

    // Which category and event
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }

    public DateTime EnrolmentDate { get; set; }

    // Pending, Confirmed or Completed
    public string Status { get; set; } = string.Empty;
}