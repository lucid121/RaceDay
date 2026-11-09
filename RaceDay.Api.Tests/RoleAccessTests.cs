using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Tests;

// Tests that each role is blocked from the other role's features
public class RoleAccessTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public RoleAccessTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        var chain = await TestHelper.CreateChainAsync(_factory);

        var response = await chain.Organiser.Client.PostAsJsonAsync("/api/enrolments",
            new EnrolmentRequestDto { CategoryId = chain.Category.CategoryId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewMyEnrolments_Returns403()
    {
        var organiser = await TestHelper.CreateUserAsync(_factory, "Organiser");

        var response = await organiser.Client.GetAsync("/api/enrolments/mine");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewMyResults_Returns403()
    {
        var organiser = await TestHelper.CreateUserAsync(_factory, "Organiser");

        var response = await organiser.Client.GetAsync("/api/results/mine");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotAddCategory_Returns403()
    {
        var chain = await TestHelper.CreateChainAsync(_factory);
        var body = new CategoryRequestDto { CategoryName = "Hack", Distance = 5, EntryFee = 0 };

        var response = await chain.Participant.Client.PostAsJsonAsync($"/api/events/{chain.Event.EventId}/categories", body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolments_Returns403()
    {
        var chain = await TestHelper.CreateChainAsync(_factory);

        var response = await chain.Participant.Client.GetAsync($"/api/events/{chain.Event.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotChangeEnrolmentStatus_Returns403()
    {
        var chain = await TestHelper.CreateChainAsync(_factory);

        var response = await chain.Participant.Client.PutAsJsonAsync(
            $"/api/enrolments/{chain.Enrolment.EnrolmentId}/status",
            new UpdateEnrolmentStatusDto { Status = "Confirmed" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotSaveResult_Returns403()
    {
        var chain = await TestHelper.CreateChainAsync(_factory);

        var response = await chain.Participant.Client.PutAsJsonAsync(
            $"/api/enrolments/{chain.Enrolment.EnrolmentId}/result",
            new ResultRequestDto { FinishTime = TimeSpan.FromMinutes(45), Position = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NotLoggedIn_CannotEnrol_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/enrolments", new EnrolmentRequestDto { CategoryId = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NotLoggedIn_CannotSaveResult_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/enrolments/1/result",
            new ResultRequestDto { FinishTime = TimeSpan.FromMinutes(45), Position = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
