using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

// The data a user sends to change their own profile.
// Note: there is no Role here, so nobody can change their own role.
public class UpdateProfileDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Surname { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;
}