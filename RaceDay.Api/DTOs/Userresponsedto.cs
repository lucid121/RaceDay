namespace RaceDay.Api.DTOs;

// The data we send BACK to the user.
// Notice: there is no PasswordHash here, so it can never leak.
public class UserResponseDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}