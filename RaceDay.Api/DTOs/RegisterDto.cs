using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

// DTO = Data Transfer Object.
// This is the data the user sends when they register.
// We use a DTO so the user can never set things like UserId or PasswordHash by themselves.
public class RegisterDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Surname { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    // The plain password the user types. We only use it to make a hash. We never save it.
    [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    // The user chooses their role: "Organiser" or "Participant"
    [Required]
    [RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Role must be Organiser or Participant.")]
    public string RoleName { get; set; } = string.Empty;
}