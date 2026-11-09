using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Tests;

// Tests for register, login and logout
public class AuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ValidOrganiser_Returns201()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", TestHelper.NewRegister("Organiser"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        Assert.Equal("Organiser", user!.Role);
    }

    [Fact]
    public async Task Register_ValidParticipant_Returns201()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", TestHelper.NewRegister("Participant"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        Assert.Equal("Participant", user!.Role);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var email = TestHelper.NewEmail();
        await client.PostAsJsonAsync("/api/auth/register", TestHelper.NewRegister("Participant", email));

        var response = await client.PostAsJsonAsync("/api/auth/register", TestHelper.NewRegister("Participant", email));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", TestHelper.NewRegister("Admin"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var client = _factory.CreateClient();
        var body = TestHelper.NewRegister("Participant");
        body.Password = "123";

        var response = await client.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_PasswordIsHashedInDatabase()
    {
        var client = _factory.CreateClient();
        var email = TestHelper.NewEmail();
        await client.PostAsJsonAsync("/api/auth/register", TestHelper.NewRegister("Participant", email));

        // Look inside the database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var saved = await db.Users.FirstAsync(u => u.Email == email);

        Assert.NotEqual(TestHelper.Password, saved.PasswordHash); // not the real password
        Assert.True(saved.PasswordHash.Length > 20);              // a long scrambled hash
    }

    [Fact]
    public async Task Login_CorrectCredentials_Returns200()
    {
        var client = _factory.CreateClient();
        var email = TestHelper.NewEmail();
        await client.PostAsJsonAsync("/api/auth/register", TestHelper.NewRegister("Participant", email));

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginDto { Email = email, Password = TestHelper.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var email = TestHelper.NewEmail();
        await client.PostAsJsonAsync("/api/auth/register", TestHelper.NewRegister("Participant", email));

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginDto { Email = email, Password = "WrongPassword999" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginDto { Email = "nobody@nowhere.com", Password = TestHelper.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Session_AfterLogin_ProfileIsAllowed()
    {
        var user = await TestHelper.CreateUserAsync(_factory, "Participant");

        var response = await user.Client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        Assert.Equal(user.Email, profile!.Email);
    }

    [Fact]
    public async Task ProtectedEndpoint_NotLoggedIn_Returns401()
    {
        var client = _factory.CreateClient(); // never logged in

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_NotLoggedIn_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_AfterLogin_EndsTheSession()
    {
        var user = await TestHelper.CreateUserAsync(_factory, "Participant");

        var logout = await user.Client.PostAsync("/api/auth/logout", null);
        var afterLogout = await user.Client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }
}
