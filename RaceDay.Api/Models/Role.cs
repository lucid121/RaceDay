using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

// Role Table
// Holds the two roles in the system: Organiser and Participant.
public class Role
{
    // Primary key (Identity 1,1 in SQL)
    public int RoleId { get; set; }

    // Role name, for example "Organiser". Must be unique.
    [Required, MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;

    // One role has many users (one-to-many)
    public List<User> Users { get; set; } = new();
}