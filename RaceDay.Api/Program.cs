using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the app.

builder.Services.AddControllers();

// Connect EF Core to SQL Server with the connection string in appsettings.json
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session needs a place to store data on the server (memory is OK for this project)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // log out after 30 minutes with no activity
    options.Cookie.HttpOnly = true;                 // JavaScript cannot read this cookie
    options.Cookie.IsEssential = true;
});

// Learn more about Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Set up the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Session must come BEFORE authorization and MapControllers
app.UseSession();

app.UseAuthorization();

app.MapControllers();

app.Run();