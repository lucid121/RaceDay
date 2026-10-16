using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;



namespace RaceDay.Api.Models
{
    [Table("Role")]
    [Index(nameof(RoleName), IsUnique = true)]
    public class Role
    {
        [Key]
        [Column("Roleid")]
        public int RoleId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("Rolename")]
        public string RoleName { get; set; } = string.Empty;

        // Navigation property
        public ICollection<User> Users { get; set; } = new List<User>();
    }

    [Table("User")]
    [Index(nameof(Email), IsUnique = true)]
    public class User
    {
        [Key]
        [Column("Userid")]
        public int UserId { get; set; }

        [Required]
        [Column("Roleid")]
        public int RoleId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("Surname")]
        public string Surname { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [Column("Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("Passwordhash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [Column("Datecreated")]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("RoleId")]
        public Role? Role { get; set; }

        public ICollection<Event> Events { get; set; } = new List<Event>();
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }

    [Table("Event")]
    public class Event
    {
        [Key]
        [Column("Eventid")]
        public int EventId { get; set; }

        [Required]
        [Column("Organiserid")]
        public int OrganiserId { get; set; }

        [Required]
        [MaxLength(150)]
        [Column("Eventname")]
        public string EventName { get; set; } = string.Empty;

        [Required]
        [Column("Eventdate", TypeName = "date")]
        public DateTime EventDate { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("Location")]
        public string Location { get; set; } = string.Empty;

        [MaxLength(1000)]
        [Column("Description")]
        public string? Description { get; set; }

        // Navigation properties
        [ForeignKey("OrganiserId")]
        public User? Organiser { get; set; }

        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }

    [Table("Category")]
    public class Category
    {
        [Key]
        [Column("Categoryid")]
        public int CategoryId { get; set; }

        [Required]
        [Column("Eventid")]
        public int EventId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("Categoryname")]
        public string CategoryName { get; set; } = string.Empty;

        [Required]
        [Column("Distance", TypeName = "decimal(5,2)")]
        public decimal Distance { get; set; }

        [Required]
        [Column("Entryfee", TypeName = "decimal(10,2)")]
        public decimal EntryFee { get; set; } = 0.00m;

        // Navigation properties
        [ForeignKey("EventId")]
        public Event? Event { get; set; }

        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }

    [Table("Enrolment")]
    [Index(nameof(UserId), nameof(CategoryId), IsUnique = true)]
    public class Enrolment
    {
        [Key]
        [Column("Enrolmentid")]
        public int EnrolmentId { get; set; }

        [Required]
        [Column("Userid")]
        public int UserId { get; set; }

        [Required]
        [Column("Categoryid")]
        public int CategoryId { get; set; }

        [Required]
        [Column("Enrolmentdate")]
        public DateTime EnrolmentDate { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(50)]
        [Column("Status")]
        public string Status { get; set; } = "Registered"; // FIXED TYPO HERE

        // Navigation properties
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        public Result? Result { get; set; }
    }

    [Table("Result")]
    [Index(nameof(EnrolmentId), IsUnique = true)]
    public class Result
    {
        [Key]
        [Column("Resultid")]
        public int ResultId { get; set; }

        [Required]
        [Column("Enrolmentid")]
        public int EnrolmentId { get; set; }

        [Column("Finishtime", TypeName = "time")]
        public TimeSpan? FinishTime { get; set; }

        [Column("Position")]
        public int? Position { get; set; }

        [MaxLength(255)]
        [Column("Remarks")]
        public string? Remarks { get; set; }

        // Navigation property
        [ForeignKey("EnrolmentId")]
        public Enrolment? Enrolment { get; set; }
    }

    public class RaceDayDbContext : DbContext
    {
        public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Enrolment> Enrolments { get; set; }
        public DbSet<Result> Results { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Delete Behaviors (No cascading delete from User to prevent accidental mass deletion)
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Organiser)
                .WithMany(u => u.Events)
                .HasForeignKey(e => e.OrganiserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.User)
                .WithMany(u => u.Enrolments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cascading Deletes: Event -> Category -> Enrolment -> Result
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Result>()
                .HasOne(r => r.Enrolment)
                .WithOne(e => e.Result)
                .HasForeignKey<Result>(r => r.EnrolmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}