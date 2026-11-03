using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

// Table User
// One row = one person who can log in (an Organiser or a Participant).
public class User
{
    // Primary key
    public int UserId { get; set; }

    // Foreign key -> Role table. Tells us if this person is Organiser or Participant.
    public int RoleId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Surname { get; set; } = string.Empty;

    // Must be unique. Used to log in.
    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    // NEVER store the real password. Store only the hashed version here.
    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    // Set automatically by the database (GETDATE()) when the user is created
    public DateTime DateCreated { get; set; }

    // Navigation: the Role this user belongs to (many users -> one role)
    public Role Role { get; set; } = null!;

    // Navigation: events this user created (only Organisers will have these)
    public List<Event> OrganisedEvents { get; set; } = new();

    // Navigation: events this user entered (only Participants will have these)
    public List<Enrolment> Enrolments { get; set; } = new();
}