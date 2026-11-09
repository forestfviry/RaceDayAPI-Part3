using static RaceDayAPI.Tests.TestHelpers;

namespace RaceDayAPI.Tests;

/// <summary>Results capture and the category leaderboard.</summary>
public class ResultTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static object FinishedResult(int position, string time) =>
        new { finishTime = time, position, status = "Finished" };

    [Fact]
    public async Task CaptureResult_ByOwningOrganiser_Returns201()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var (participant, _) = await CreateUserClientAsync(factory, "Participant");
        var enrolment = await EnrolAsync(participant, category.CategoryId);

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result",
            FinishedResult(1, "00:52:10"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ResultDto>();
        Assert.Equal(1, result!.Position);
        Assert.Equal("Finished", result.Status);
        Assert.Equal(new TimeSpan(0, 52, 10), result.FinishTime);
    }

    [Fact]
    public async Task CaptureResult_AsParticipant_Returns403()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var (participant, _) = await CreateUserClientAsync(factory, "Participant");
        var enrolment = await EnrolAsync(participant, category.CategoryId);

        var response = await participant.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result",
            FinishedResult(1, "00:30:00"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ByOtherOrganiser_Returns403()
    {
        var (owner, _) = await CreateUserClientAsync(factory, "Organiser");
        var (other, _) = await CreateUserClientAsync(factory, "Organiser");
        var ev = await CreateEventAsync(owner);
        var category = await CreateCategoryAsync(owner, ev.EventId);
        var (participant, _) = await CreateUserClientAsync(factory, "Participant");
        var enrolment = await EnrolAsync(participant, category.CategoryId);

        var response = await other.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result",
            FinishedResult(1, "00:30:00"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_EnrolmentMissing_Returns404()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/enrolments/999999/result", FinishedResult(1, "00:30:00"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var (participant, _) = await CreateUserClientAsync(factory, "Participant");
        var enrolment = await EnrolAsync(participant, category.CategoryId);
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", FinishedResult(1, "00:30:00"));

        var second = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", FinishedResult(2, "00:31:00"));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_InvalidStatus_Returns400()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var (participant, _) = await CreateUserClientAsync(factory, "Participant");
        var enrolment = await EnrolAsync(participant, category.CategoryId);

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result",
            new { finishTime = "00:30:00", position = 1, status = "Banana" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateResult_ByOwningOrganiser_Returns200_WithCorrection()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var (participant, _) = await CreateUserClientAsync(factory, "Participant");
        var enrolment = await EnrolAsync(participant, category.CategoryId);
        var created = await (await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result",
            FinishedResult(2, "00:40:00"))).Content.ReadFromJsonAsync<ResultDto>();

        var response = await organiser.PutAsJsonAsync($"/api/results/{created!.ResultId}", FinishedResult(1, "00:38:00"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ResultDto>();
        Assert.Equal(1, updated!.Position);
    }

    [Fact]
    public async Task UpdateResult_AsParticipant_Returns403()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var (participant, _) = await CreateUserClientAsync(factory, "Participant");
        var enrolment = await EnrolAsync(participant, category.CategoryId);
        var created = await (await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result",
            FinishedResult(1, "00:40:00"))).Content.ReadFromJsonAsync<ResultDto>();

        var response = await participant.PutAsJsonAsync($"/api/results/{created!.ResultId}", FinishedResult(1, "00:01:00"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateResult_Missing_Returns404()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");

        var response = await organiser.PutAsJsonAsync("/api/results/999999", FinishedResult(1, "00:30:00"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Leaderboard_Anonymous_IsOrderedByPosition()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var (p1, _) = await CreateUserClientAsync(factory, "Participant");
        var (p2, _) = await CreateUserClientAsync(factory, "Participant");
        var e1 = await EnrolAsync(p1, category.CategoryId);
        var e2 = await EnrolAsync(p2, category.CategoryId);

        // Capture the second-place result first to prove ordering is not insertion order
        await organiser.PostAsJsonAsync($"/api/enrolments/{e1.EnrolmentId}/result", FinishedResult(2, "00:55:00"));
        await organiser.PostAsJsonAsync($"/api/enrolments/{e2.EnrolmentId}/result", FinishedResult(1, "00:50:00"));

        var response = await factory.CreateClient().GetAsync($"/api/categories/{category.CategoryId}/result");
        var board = await response.Content.ReadFromJsonAsync<List<ResultDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, board!.Count);
        Assert.Equal(1, board[0].Position);
        Assert.Equal(e2.EnrolmentId, board[0].EnrolmentId);
        Assert.Equal(2, board[1].Position);
    }

    [Fact]
    public async Task Leaderboard_CategoryMissing_Returns404()
    {
        var response = await factory.CreateClient().GetAsync("/api/categories/999999/result");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
