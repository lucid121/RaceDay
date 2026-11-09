using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Tests;

// Tests for event management
public class EventTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EventTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetEvents_NotLoggedIn_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CreateEvent_Returns201()
    {
        var organiser = await TestHelper.CreateUserAsync(_factory, "Organiser");

        var response = await organiser.Client.PostAsJsonAsync("/api/events", TestHelper.NewEventRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.Equal(organiser.UserId, created!.OrganiserId); // the organiser is the logged-in user
    }

    [Fact]
    public async Task Organiser_CreateEvent_PastDate_Returns400()
    {
        var organiser = await TestHelper.CreateUserAsync(_factory, "Organiser");

        var response = await organiser.Client.PostAsJsonAsync("/api/events", TestHelper.NewEventRequest(daysAhead: -5));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CreateEvent_InvalidType_Returns400()
    {
        var organiser = await TestHelper.CreateUserAsync(_factory, "Organiser");

        var response = await organiser.Client.PostAsJsonAsync("/api/events", TestHelper.NewEventRequest(eventType: "Swim"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewOwnEvents()
    {
        var organiser = await TestHelper.CreateUserAsync(_factory, "Organiser");
        var ev = await TestHelper.CreateEventAsync(organiser.Client);

        var response = await organiser.Client.GetAsync("/api/events/mine");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<EventDto>>();
        Assert.Contains(list!, e => e.EventId == ev.EventId);
    }

    [Fact]
    public async Task Participant_CanViewEvents_Returns200()
    {
        var organiser = await TestHelper.CreateUserAsync(_factory, "Organiser");
        var ev = await TestHelper.CreateEventAsync(organiser.Client);
        var participant = await TestHelper.CreateUserAsync(_factory, "Participant");

        var response = await participant.Client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<EventDto>>();
        Assert.Contains(list!, e => e.EventId == ev.EventId);
    }

    [Fact]
    public async Task Participant_CreateEvent_Returns403()
    {
        var participant = await TestHelper.CreateUserAsync(_factory, "Participant");

        var response = await participant.Client.PostAsJsonAsync("/api/events", TestHelper.NewEventRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_UpdateOwnEvent_Returns200()
    {
        var organiser = await TestHelper.CreateUserAsync(_factory, "Organiser");
        var ev = await TestHelper.CreateEventAsync(organiser.Client);
        var update = TestHelper.NewEventRequest();
        update.EventName = "Changed Name";

        var response = await organiser.Client.PutAsJsonAsync($"/api/events/{ev.EventId}", update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.Equal("Changed Name", updated!.EventName);
    }

    [Fact]
    public async Task Organiser_UpdateAnotherOrganisersEvent_Returns403()
    {
        var owner = await TestHelper.CreateUserAsync(_factory, "Organiser");
        var otherOrganiser = await TestHelper.CreateUserAsync(_factory, "Organiser");
        var ev = await TestHelper.CreateEventAsync(owner.Client);

        var response = await otherOrganiser.Client.PutAsJsonAsync($"/api/events/{ev.EventId}", TestHelper.NewEventRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_UpdateMissingEvent_Returns404()
    {
        var organiser = await TestHelper.CreateUserAsync(_factory, "Organiser");

        var response = await organiser.Client.PutAsJsonAsync("/api/events/999999", TestHelper.NewEventRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_DeleteOwnEvent_Returns204_ThenEventIsGone()
    {
        var organiser = await TestHelper.CreateUserAsync(_factory, "Organiser");
        var ev = await TestHelper.CreateEventAsync(organiser.Client);

        var delete = await organiser.Client.DeleteAsync($"/api/events/{ev.EventId}");
        var get = await organiser.Client.GetAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Participant_DeleteEvent_Returns403()
    {
        var organiser = await TestHelper.CreateUserAsync(_factory, "Organiser");
        var ev = await TestHelper.CreateEventAsync(organiser.Client);
        var participant = await TestHelper.CreateUserAsync(_factory, "Participant");

        var response = await participant.Client.DeleteAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
