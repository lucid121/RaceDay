using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

// What a Participant sends to enter an event.
// They only say WHICH category. The category already knows its event.
public class EnrolmentRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Please give a valid CategoryId.")]
    public int CategoryId { get; set; }
}