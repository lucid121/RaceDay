using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

// What an Organiser sends to change the status of an enrolment
public class UpdateEnrolmentStatusDto
{
    [Required]
    [RegularExpression("^(Pending|Confirmed|Completed)$", ErrorMessage = "Status must be Pending, Confirmed or Completed.")]
    public string Status { get; set; } = string.Empty;
}