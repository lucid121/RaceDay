using RaceDay.Api.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1. Register DbContext
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Register Session Services (REQUIRED before app.UseSession())
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Session middleware MUST come before UseAuthorization and MapControllers
app.UseSession();

app.UseAuthorization();
app.MapControllers();

// Database Seeding
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    db.Database.EnsureCreated();

    if (!db.Roles.Any())
    {
        db.Roles.AddRange(
            new Role { RoleId = 1, RoleName = "Organiser" },
            new Role { RoleId = 2, RoleName = "Participant" }
        );
        db.SaveChanges();
    }

    if (!db.Users.Any())
    {
        db.Users.Add(new User
        {
            RoleId = 1,
            Name = "Admin",
            Surname = "Organiser",
            Email = "organiser@raceday.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            DateCreated = DateTime.Now
        });
        db.SaveChanges();
    }
}

app.Run();

public partial class Program { }