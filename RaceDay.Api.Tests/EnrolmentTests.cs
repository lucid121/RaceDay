using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Tests;

// Tests for entering events and viewing enrolments
public class EnrolmentTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EnrolmentTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Participant_Enrol_Returns201_AndIsLinkedCorrectly()
    {
        var organiser = await TestHelper.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelper.CreateUserAsync(_factory, "Participant");
        var ev = await TestHelper.CreateEventAsync(organiser.Client);
        var category = await TestHelper.CreateCategoryAsync(organiser.Client, ev.EventId);

        var response = await participant.Client.PostAsJsonAsync("/api/enrolments",
            new EnrolmentRequestDto { CategoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentDto>();
        Assert.Equal(participant.UserId, enrolment!.UserId);        // the right participant
        Assert.Equal(category.CategoryId, enrolment.CategoryId);    // the right category
        Assert.Equal(ev.EventId, enrolment.EventId);                // the right event
        Assert.Equal("Pending", enrolment.Status);                  // starts as Pending
    }

    [Fact]
    public async Task Participant_EnrolTwiceInSameCategory_Returns409()
    {
        var chain = await TestHelper.CreateChainAsync(_factory);

        var response = await chain.Participant.Client.PostAsJsonAsync("/api/enrolments",
            new EnrolmentRequestDto { CategoryId = chain.Category.CategoryId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Participant_EnrolInMissingCategory_Returns404()
    {
        var participant = await TestHelper.CreateUserAsync(_factory, "Participant");

        var response = await participant.Client.PostAsJsonAsync("/api/enrolments",
            new EnrolmentRequestDto { CategoryId = 999999 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_SeesOnlyOwnEnrolments()
    {
        var chain = await TestHelper.CreateChainAsync(_factory);
        var otherParticipant = await TestHelper.CreateUserAsync(_factory, "Participant");

        var mine = await chain.Participant.Client.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/mine");
        var theirs = await otherParticipant.Client.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/mine");

        Assert.Contains(mine!, e => e.EnrolmentId == chain.Enrolment.EnrolmentId);
        Assert.Empty(theirs!);
    }

    [Fact]
    public async Task Organiser_SeesEnrolmentsOfOwnEvent()
    {
        var chain = await TestHelper.CreateChainAsync(_factory);

        var response = await chain.Organiser.Client.GetAsync($"/api/events/{chain.Event.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<EnrolmentDto>>();
        Assert.Contains(list!, e => e.EnrolmentId == chain.Enrolment.EnrolmentId);
    }

    [Fact]
    public async Task Organiser_CannotSeeEnrolmentsOfAnotherOrganisersEvent_Returns403()
    {
        var chain = await TestHelper.CreateChainAsync(_factory);
        var otherOrganiser = await TestHelper.CreateUserAsync(_factory, "Organiser");

        var response = await otherOrganiser.Client.GetAsync($"/api/events/{chain.Event.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_ConfirmEnrolment_Returns200()
    {
        var chain = await TestHelper.CreateChainAsync(_factory);

        var response = await chain.Organiser.Client.PutAsJsonAsync(
            $"/api/enrolments/{chain.Enrolment.EnrolmentId}/status",
            new UpdateEnrolmentStatusDto { Status = "Confirmed" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EnrolmentDto>();
        Assert.Equal("Confirmed", updated!.Status);
    }

    [Fact]
    public async Task Organiser_InvalidStatus_Returns400()
    {
        var chain = await TestHelper.CreateChainAsync(_factory);

        var response = await chain.Organiser.Client.PutAsJsonAsync(
            $"/api/enrolments/{chain.Enrolment.EnrolmentId}/status",
            new UpdateEnrolmentStatusDto { Status = "Banana" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
