using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

// This class starts the real API inside the tests.
// The ONLY difference: it uses a temporary in-memory database instead of SQL Server.
// So the tests run fast, need no SQL Server, and also work on GitHub Actions.
public class ApiFactory : WebApplicationFactory<Program>
{
    // A new random name = a fresh, empty database for each factory
    private readonly string _dbName = "RaceDayTests_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server setup, then add the in-memory one
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();
            services.AddDbContext<RaceDayDbContext>(options => options.UseInMemoryDatabase(_dbName));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Create the tables (and the two seeded roles) in the in-memory database
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<RaceDayDbContext>().Database.EnsureCreated();

        return host;
    }
}