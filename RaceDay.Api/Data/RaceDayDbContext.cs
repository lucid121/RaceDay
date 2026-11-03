using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

// The DbContext is the "bridge" between C# and SQL Server.
// Each DbSet below = one table in the database.
public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    // Here we describe the rules so the tables match the SQL script exactly.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Your SQL uses Varchar (not Nvarchar), so make every text column Varchar.
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(e => e.GetProperties())
                     .Where(p => p.ClrType == typeof(string)))
        {
            property.SetIsUnicode(false);
        }

        // ---------- ROLE ----------
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role");

            // RoleName must be unique
            entity.HasIndex(r => r.RoleName).IsUnique();

            // Always create the two roles so registration works straight away
            entity.HasData(
                new Role { RoleId = 1, RoleName = "Organiser" },
                new Role { RoleId = 2, RoleName = "Participant" });
        });

        // ---------- USER ----------
        modelBuilder.Entity<User>(entity =>
        {
            // "User" is a special word in SQL, EF adds [ ] for us
            entity.ToTable("User");

            // Email must be unique
            entity.HasIndex(u => u.Email).IsUnique();

            // Database fills in the date automatically
            entity.Property(u => u.DateCreated).HasDefaultValueSql("GETDATE()");

            // User -> Role (many-to-one). Do not delete users when a role is deleted.
            entity.HasOne(u => u.Role)
                  .WithMany(r => r.Users)
                  .HasForeignKey(u => u.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- EVENT ----------
        modelBuilder.Entity<Event>(entity =>
        {
            // Check rule: EventType can only be Run, Walk or Cycle
            entity.ToTable("Event", t => t.HasCheckConstraint(
                "Ck_Event_Type", "[EventType] IN ('Run','Walk','Cycle')"));

            // Event -> Organiser (many-to-one). Do not delete events when a user is deleted.
            entity.HasOne(e => e.Organiser)
                  .WithMany(u => u.OrganisedEvents)
                  .HasForeignKey(e => e.OrganiserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- CATEGORY ----------
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Category");

            // Default entry fee is 0.00
            entity.Property(c => c.EntryFee).HasDefaultValue(0.00m);

            // Category -> Event (many-to-one). Deleting an event deletes its categories.
            entity.HasOne(c => c.Event)
                  .WithMany(e => e.Categories)
                  .HasForeignKey(c => c.EventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- ENROLMENT ----------
        modelBuilder.Entity<Enrolment>(entity =>
        {
            // Check rule: Status can only be Pending, Confirmed or Completed
            entity.ToTable("Enrolment", t => t.HasCheckConstraint(
                "Ck_Enrolment_Status", "[Status] IN ('Pending','Confirmed','Completed')"));

            // Defaults set by the database
            entity.Property(e => e.Status).HasDefaultValue("Pending");
            entity.Property(e => e.EnrolmentDate).HasDefaultValueSql("GETDATE()");

            // A user cannot enter the same category twice
            entity.HasIndex(e => new { e.UserId, e.CategoryId })
                  .IsUnique()
                  .HasDatabaseName("Uq_User_Category");

            // Enrolment -> User (many-to-one)
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Enrolments)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Enrolment -> Category (many-to-one). Deleting a category deletes its enrolments.
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Enrolments)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- RESULT ----------
        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Result");

            // Enrolment -> Result is one-to-one. Deleting an enrolment deletes its result.
            // (This also makes EnrolmentId unique, like in your SQL.)
            entity.HasOne(r => r.Enrolment)
                  .WithOne(e => e.Result)
                  .HasForeignKey<Result>(r => r.EnrolmentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}