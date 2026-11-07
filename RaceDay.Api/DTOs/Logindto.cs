using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

// The data the user sends when they log in
public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}