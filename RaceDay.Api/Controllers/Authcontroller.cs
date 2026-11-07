using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Handles creating accounts, logging in and logging out.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    // This tool changes a password into a safe code called a hash.
    // It can also check if a password matches the saved hash.
    private readonly PasswordHasher<User> _hasher = new();

    // The app gives us the database so we can use it here.
    // This is called dependency injection.
    public AuthController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Creates a new account as an Organiser or a Participant.
    /// </summary>
    /// <remarks>Send name, surname, email, password (min 8 characters) and roleName.</remarks>
    /// <response code="201">Account created.</response>
    /// <response code="400">Some of the data is not valid.</response>
    /// <response code="409">This email is already used.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        // Remove extra spaces and make all letters lowercase.
        // This makes "Sipho@Mail.com " and "sipho@mail.com" the same email.
        var email = dto.Email.Trim().ToLower();

        // Step 1: Check if someone is already using this email.
        var emailExists = await _db.Users.AnyAsync(u => u.Email == email);
        if (emailExists)
        {
            return Conflict(new { message = "This email is already registered." });
        }

        // Step 2: Find the user's role in the database.
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == dto.RoleName);
        if (role == null)
        {
            return BadRequest(new { message = "Role must be Organiser or Participant." });
        }

        // Step 3: Create a new user and add their details.
        var user = new User
        {
            Name = dto.Name.Trim(),
            Surname = dto.Surname.Trim(),
            Email = email,
            RoleId = role.RoleId
        };

        // Step 4: Turn the password into a safe hash.
        // We save the hash, not the real password.
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        // Step 5: Add the new user to the database and save the changes.
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Step 6: Send the user's details back without sending the password.
        var response = new UserResponseDto
        {
            UserId = user.UserId,
            Name = user.Name,
            Surname = user.Surname,
            Email = user.Email,
            Role = role.RoleName
        };
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Logs in using an email and password.
    /// The server uses a session to remember the user.
    /// </summary>
    /// <response code="200">Logged in. Your user details are returned.</response>
    /// <response code="401">Wrong email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLower();

        // Step 1: Find the user using their email.
        // Include also gets the user's role from the database.
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        // Step 2: Check if the password is correct.
        // If the user does not exist OR the password is wrong,
        // show the same error message to keep the account safe.
        if (user == null ||
            _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // Step 3: Save the user's ID and role in the session.
        // This allows the server to remember the user.
        // The user does not need to log in again for every request.
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role.RoleName);

        return Ok(new UserResponseDto
        {
            UserId = user.UserId,
            Name = user.Name,
            Surname = user.Surname,
            Email = user.Email,
            Role = user.Role.RoleName
        });
    }

    /// <summary>
    /// Logs out the user and clears their session.
    /// </summary>
    /// <response code="200">Logged out.</response>
    /// <response code="401">You were not logged in.</response>
    [HttpPost("logout")]
    [SessionAuthorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        // Remove all the information saved in the session.
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out." });
    }
}