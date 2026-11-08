using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Connect EF Core to SQL Server using the connection string in appsettings.json
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session needs a place to keep data on the server (memory is fine for this project)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // log out after 30 min of no activity
    options.Cookie.HttpOnly = true;                 // JavaScript cannot read the cookie
    options.Cookie.IsEssential = true;
});

// Swagger shows every endpoint in the browser
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Show the /// comments from the controllers in Swagger (only if the XML file exists)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
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

// This line lets the test project start the API in memory. Do not remove it.
public partial class Program { }