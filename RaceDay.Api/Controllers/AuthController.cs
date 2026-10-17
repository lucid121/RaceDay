using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayDbContext _db;

        public AuthController(RaceDayDbContext db)
        {
            _db = db;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            // 1. Check if role is valid (1 or 2)
            if (request.RoleId != 1 && request.RoleId != 2)
            {
                return BadRequest(new { message = "Role must be 1 (Organiser) or 2 (Participant)" });
            }

            // 2. Check if email already exists
            if (await _db.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
            {
                return Conflict(new { message = "Email already registered" }); // 409
            }

            // 3. Create new user with hashed password
            var newUser = new User
            {
                RoleId = request.RoleId,
                Name = request.Name,
                Surname = request.Surname,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                DateCreated = DateTime.Now
            };

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();

            // 4. Log them in by setting the session
            HttpContext.Session.SetInt32("UserId", newUser.UserId);
            HttpContext.Session.SetInt32("RoleId", newUser.RoleId);

            // 5. Return 201 Created
            return CreatedAtAction(nameof(Register), new
            {
                userId = newUser.UserId,
                email = newUser.Email,
                token = HttpContext.Session.Id
            });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            // 1. Find user by email
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
            if (user == null)
            {
                return NotFound(new { message = "User not found" }); // 404
            }

            // 2. Check password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid password" }); // 401
            }

            // 3. Set session
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetInt32("RoleId", user.RoleId);

            // 4. Return 200 OK
            return Ok(new
            {
                token = HttpContext.Session.Id,
                userId = user.UserId,
                roleId = user.RoleId
            });
        }
    }

    // DTOs (Data Transfer Objects) to keep passwords out of responses
    public class RegisterDto
    {
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int RoleId { get; set; }
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}