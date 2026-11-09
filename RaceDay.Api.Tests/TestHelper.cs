using System.Net.Http.Json;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Tests;

// A logged-in user for a test. Client = a browser that is already logged in.
public record TestUser(HttpClient Client, string Email, int UserId);

// An Organiser, a Participant, and an enrolment that links them
public record EnrolmentChain(TestUser Organiser, TestUser Participant, EventDto Event, CategoryDto Category, EnrolmentDto Enrolment);

// Small shortcuts so the tests stay short and easy to read
public static class TestHelper
{
    public const string Password = "Password123";

    // A different email each time, so tests never clash
    public static string NewEmail() => $"user_{Guid.NewGuid():N}@test.com";

    public static RegisterDto NewRegister(string role, string? email = null)
    {
        return new RegisterDto
        {
            Name = "Test",
            Surname = role,
            Email = email ?? NewEmail(),
            Password = Password,
            RoleName = role
        };
    }

    public static EventRequestDto NewEventRequest(int daysAhead = 30, string eventType = "Run")
    {
        return new EventRequestDto
        {
            EventName = "Test Event",
            EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(daysAhead)),
            Location = "Cape Town",
            Distance = 10,
            EventType = eventType,
            Description = "Test description"
        };
    }

    // Register a new user AND log in. The returned client keeps the session cookie.
    public static async Task<TestUser> CreateUserAsync(ApiFactory factory, string role)
    {
        var client = factory.CreateClient();
        var email = NewEmail();

        var register = await client.PostAsJsonAsync("/api/auth/register", NewRegister(role, email));
        register.EnsureSuccessStatusCode();
        var user = await register.Content.ReadFromJsonAsync<UserResponseDto>();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = email, Password = Password });
        login.EnsureSuccessStatusCode();

        return new TestUser(client, email, user!.UserId);
    }

    public static async Task<EventDto> CreateEventAsync(HttpClient organiser)
    {
        var response = await organiser.PostAsJsonAsync("/api/events", NewEventRequest());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDto>())!;
    }

    public static async Task<CategoryDto> CreateCategoryAsync(HttpClient organiser, int eventId)
    {
        var body = new CategoryRequestDto { CategoryName = "10km Run", Distance = 10, EntryFee = 150 };
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    public static async Task<EnrolmentDto> EnrolAsync(HttpClient participant, int categoryId)
    {
        var response = await participant.PostAsJsonAsync("/api/enrolments", new EnrolmentRequestDto { CategoryId = categoryId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EnrolmentDto>())!;
    }

    // Builds everything: Organiser + event + category + Participant + enrolment
    public static async Task<EnrolmentChain> CreateChainAsync(ApiFactory factory)
    {
        var organiser = await CreateUserAsync(factory, "Organiser");
        var participant = await CreateUserAsync(factory, "Participant");
        var ev = await CreateEventAsync(organiser.Client);
        var category = await CreateCategoryAsync(organiser.Client, ev.EventId);
        var enrolment = await EnrolAsync(participant.Client, category.CategoryId);
        return new EnrolmentChain(organiser, participant, ev, category, enrolment);
    }
}
