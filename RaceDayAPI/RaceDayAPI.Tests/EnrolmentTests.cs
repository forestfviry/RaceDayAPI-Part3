using static RaceDayAPI.Tests.TestHelpers;

namespace RaceDayAPI.Tests;

/// <summary>Event enrolments: participants can enrol, the link is recorded, and roles are enforced.</summary>
public class EnrolmentTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<(HttpClient Organiser, EventDto Event, CategoryDto Category)> SetUpRaceAsync(int maxParticipants = 100)
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId, maxParticipants);
        return (organiser, ev, category);
    }

    [Fact]
    public async Task Enrol_AsParticipant_Returns201_AndRecordsParticipantEventAndCategory()
    {
        var (_, ev, category) = await SetUpRaceAsync();
        var (participant, user) = await CreateUserClientAsync(factory, "Participant");

        var response = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentDto>();
        Assert.Equal(user.UserId, enrolment!.UserId);
        Assert.Equal(category.CategoryId, enrolment.CategoryId);
        Assert.Equal(ev.EventId, enrolment.EventId);
        Assert.Equal("Registered", enrolment.Status);
    }

    [Fact]
    public async Task Enrol_ThenListMine_ContainsTheEnrolment()
    {
        var (_, _, category) = await SetUpRaceAsync();
        var (participant, _) = await CreateUserClientAsync(factory, "Participant");
        var enrolment = await EnrolAsync(participant, category.CategoryId);

        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/users/me/enrolments");

        Assert.Contains(mine!, e => e.EnrolmentId == enrolment.EnrolmentId);
    }

    [Fact]
    public async Task Enrol_SameCategoryTwice_Returns409()
    {
        var (_, _, category) = await SetUpRaceAsync();
        var (participant, _) = await CreateUserClientAsync(factory, "Participant");
        await EnrolAsync(participant, category.CategoryId);

        var second = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenCategoryIsFull_Returns409()
    {
        var (_, _, category) = await SetUpRaceAsync(maxParticipants: 1);
        var (first, _) = await CreateUserClientAsync(factory, "Participant");
        var (second, _) = await CreateUserClientAsync(factory, "Participant");
        await EnrolAsync(first, category.CategoryId);

        var response = await second.PostAsync($"/api/categories/{category.CategoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_CategoryMissing_Returns404()
    {
        var (participant, _) = await CreateUserClientAsync(factory, "Participant");

        var response = await participant.PostAsync("/api/categories/999999/enrol", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_Returns403()
    {
        var (organiser, _, category) = await SetUpRaceAsync();

        var response = await organiser.PostAsync($"/api/categories/{category.CategoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_Unauthenticated_Returns401()
    {
        var (_, _, category) = await SetUpRaceAsync();

        var response = await factory.CreateClient().PostAsync($"/api/categories/{category.CategoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListMyEnrolments_AsOrganiser_Returns403()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");

        var response = await organiser.GetAsync("/api/users/me/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ViewCategoryEnrolments_ByOwningOrganiser_Returns200_WithParticipants()
    {
        var (organiser, _, category) = await SetUpRaceAsync();
        var (participant, user) = await CreateUserClientAsync(factory, "Participant");
        await EnrolAsync(participant, category.CategoryId);

        var response = await organiser.GetAsync($"/api/categories/{category.CategoryId}/enrolments");
        var list = await response.Content.ReadFromJsonAsync<List<EnrolmentDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(list!);
        Assert.Equal(user.UserId, list![0].UserId);
    }

    [Fact]
    public async Task ViewCategoryEnrolments_ByOtherOrganiser_Returns403()
    {
        var (_, _, category) = await SetUpRaceAsync();
        var (other, _) = await CreateUserClientAsync(factory, "Organiser");

        var response = await other.GetAsync($"/api/categories/{category.CategoryId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ViewCategoryEnrolments_AsParticipant_Returns403()
    {
        var (_, _, category) = await SetUpRaceAsync();
        var (participant, _) = await CreateUserClientAsync(factory, "Participant");

        var response = await participant.GetAsync($"/api/categories/{category.CategoryId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CancelEnrolment_ByOwner_Returns204_AndIsRemoved()
    {
        var (_, _, category) = await SetUpRaceAsync();
        var (participant, _) = await CreateUserClientAsync(factory, "Participant");
        var enrolment = await EnrolAsync(participant, category.CategoryId);

        var delete = await participant.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");
        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/users/me/enrolments");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.DoesNotContain(mine!, e => e.EnrolmentId == enrolment.EnrolmentId);
    }

    [Fact]
    public async Task CancelEnrolment_ByDifferentParticipant_Returns403()
    {
        var (_, _, category) = await SetUpRaceAsync();
        var (owner, _) = await CreateUserClientAsync(factory, "Participant");
        var (other, _) = await CreateUserClientAsync(factory, "Participant");
        var enrolment = await EnrolAsync(owner, category.CategoryId);

        var response = await other.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CancelEnrolment_Missing_Returns404()
    {
        var (participant, _) = await CreateUserClientAsync(factory, "Participant");

        var response = await participant.DeleteAsync("/api/enrolments/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
