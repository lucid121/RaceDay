using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Tests;

// Tests for capturing and viewing results
public class ResultTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ResultTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static ResultRequestDto NewResult(int minutes = 45, int position = 1)
    {
        return new ResultRequestDto { FinishTime = TimeSpan.FromMinutes(minutes), Position = position, Remarks = "Good run" };
    }

    [Fact]
    public async Task Organiser_SaveResult_Returns201()
    {
        var chain = await TestHelper.CreateChainAsync(_factory);

        var response = await chain.Organiser.Client.PutAsJsonAsync(
            $"/api/enrolments/{chain.Enrolment.EnrolmentId}/result", NewResult());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ResultDto>();
        Assert.Equal(1, result!.Position);
        Assert.Equal(TimeSpan.FromMinutes(45), result.FinishTime);
    }

    [Fact]
    public async Task Organiser_SaveResultAgain_UpdatesIt_Returns200()
    {
        var chain = await TestHelper.CreateChainAsync(_factory);
        var url = $"/api/enrolments/{chain.Enrolment.EnrolmentId}/result";
        await chain.Organiser.Client.PutAsJsonAsync(url, NewResult(45, 1));

        var response = await chain.Organiser.Client.PutAsJsonAsync(url, NewResult(50, 2));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ResultDto>();
        Assert.Equal(2, result!.Position);
    }

    [Fact]
    public async Task Organiser_SaveResult_InvalidFinishTime_Returns400()
    {
        var chain = await TestHelper.CreateChainAsync(_factory);

        var response = await chain.Organiser.Client.PutAsJsonAsync(
            $"/api/enrolments/{chain.Enrolment.EnrolmentId}/result",
            new ResultRequestDto { FinishTime = TimeSpan.Zero, Position = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_SaveResult_ForAnotherOrganisersEvent_Returns403()
    {
        var chain = await TestHelper.CreateChainAsync(_factory);
        var otherOrganiser = await TestHelper.CreateUserAsync(_factory, "Organiser");

        var response = await otherOrganiser.Client.PutAsJsonAsync(
            $"/api/enrolments/{chain.Enrolment.EnrolmentId}/result", NewResult());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_SaveResult_MissingEnrolment_Returns404()
    {
        var organiser = await TestHelper.CreateUserAsync(_factory, "Organiser");

        var response = await organiser.Client.PutAsJsonAsync("/api/enrolments/999999/result", NewResult());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_SeesOwnResult_AndStatusBecomesCompleted()
    {
        var chain = await TestHelper.CreateChainAsync(_factory);
        await chain.Organiser.Client.PutAsJsonAsync(
            $"/api/enrolments/{chain.Enrolment.EnrolmentId}/result", NewResult(45, 12));

        var results = await chain.Participant.Client.GetFromJsonAsync<List<ResultDto>>("/api/results/mine");
        var enrolments = await chain.Participant.Client.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/mine");

        var mine = Assert.Single(results!);
        Assert.Equal(12, mine.Position);
        Assert.Equal(1, mine.TotalFinishers);
        Assert.Equal("Completed", enrolments!.Single().Status);
    }

    [Fact]
    public async Task Organiser_CanViewResultsOfOwnEvent()
    {
        var chain = await TestHelper.CreateChainAsync(_factory);
        await chain.Organiser.Client.PutAsJsonAsync(
            $"/api/enrolments/{chain.Enrolment.EnrolmentId}/result", NewResult());

        var response = await chain.Organiser.Client.GetAsync($"/api/events/{chain.Event.EventId}/results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<ResultDto>>();
        Assert.Single(list!);
    }
}
