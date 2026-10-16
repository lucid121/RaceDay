namespace RaceDay.Api.DTOs
{
    public class RegisterDto
    {
        public int RoleId { get; set; } // 1 = Organiser, 2 = Participant
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string Token { get; set; } = string.Empty;
    }
   
}
