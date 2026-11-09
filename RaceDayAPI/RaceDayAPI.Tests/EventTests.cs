using static RaceDayAPI.Tests.TestHelpers;

namespace RaceDayAPI.Tests;

/// <summary>Event and category management, plus role-based access to those endpoints.</summary>
public class EventTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static object ValidEvent(string name = "Updated Run") => new
    {
        eventName = name,
        eventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(45)),
        location = "Soweto, Gauteng",
        description = "Test"
    };

    // ---------- Public reads ----------

    [Fact]
    public async Task GetEvents_Anonymous_Returns200_AndIncludesUpcomingEvent()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");
        var created = await CreateEventAsync(organiser, "Visible Run");

        var response = await factory.CreateClient().GetAsync("/api/events");
        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(events!, e => e.EventId == created.EventId);
    }

    [Fact]
    public async Task GetEventById_Existing_Returns200()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");
        var created = await CreateEventAsync(organiser);

        var response = await factory.CreateClient().GetAsync($"/api/events/{created.EventId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEventById_Missing_Returns404()
    {
        var response = await factory.CreateClient().GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- Create ----------

    [Fact]
    public async Task CreateEvent_AsOrganiser_Returns201_AndOwnsTheEvent()
    {
        var (organiser, user) = await CreateUserClientAsync(factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", ValidEvent("New Run"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ev = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.Equal(user.UserId, ev!.OrganiserId);
        Assert.Equal("New Run", ev.EventName);
    }

    [Fact]
    public async Task CreateEvent_Unauthenticated_Returns401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/events", ValidEvent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_Returns403()
    {
        var (participant, _) = await CreateUserClientAsync(factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/events", ValidEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_MissingName_Returns400()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventName = "",
            eventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            location = "Cape Town"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_DateInThePast_Returns400()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventName = "Old Run",
            eventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
            location = "Cape Town"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Update ----------

    [Fact]
    public async Task UpdateEvent_ByOwner_Returns200_WithNewValues()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");
        var ev = await CreateEventAsync(organiser);

        var response = await organiser.PutAsJsonAsync($"/api/events/{ev.EventId}", ValidEvent("Renamed Run"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.Equal("Renamed Run", updated!.EventName);
    }

    [Fact]
    public async Task UpdateEvent_ByDifferentOrganiser_Returns403()
    {
        var (owner, _) = await CreateUserClientAsync(factory, "Organiser");
        var (other, _) = await CreateUserClientAsync(factory, "Organiser");
        var ev = await CreateEventAsync(owner);

        var response = await other.PutAsJsonAsync($"/api/events/{ev.EventId}", ValidEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_ByParticipant_Returns403()
    {
        var (owner, _) = await CreateUserClientAsync(factory, "Organiser");
        var (participant, _) = await CreateUserClientAsync(factory, "Participant");
        var ev = await CreateEventAsync(owner);

        var response = await participant.PutAsJsonAsync($"/api/events/{ev.EventId}", ValidEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_Missing_Returns404()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");

        var response = await organiser.PutAsJsonAsync("/api/events/999999", ValidEvent());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- Delete ----------

    [Fact]
    public async Task DeleteEvent_ByOwner_Returns204_AndEventIsGone()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");
        var ev = await CreateEventAsync(organiser);
        await CreateCategoryAsync(organiser, ev.EventId);

        var delete = await organiser.DeleteAsync($"/api/events/{ev.EventId}");
        var get = await factory.CreateClient().GetAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_ByDifferentOrganiser_Returns403()
    {
        var (owner, _) = await CreateUserClientAsync(factory, "Organiser");
        var (other, _) = await CreateUserClientAsync(factory, "Organiser");
        var ev = await CreateEventAsync(owner);

        var response = await other.DeleteAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_ByParticipant_Returns403()
    {
        var (owner, _) = await CreateUserClientAsync(factory, "Organiser");
        var (participant, _) = await CreateUserClientAsync(factory, "Participant");
        var ev = await CreateEventAsync(owner);

        var response = await participant.DeleteAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_Unauthenticated_Returns401()
    {
        var response = await factory.CreateClient().DeleteAsync("/api/events/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Categories ----------

    [Fact]
    public async Task CreateCategory_ByOwner_Returns201()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");
        var ev = await CreateEventAsync(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{ev.EventId}/categories",
            new { categoryName = "5km Fun Run", distanceKm = 5m, maxParticipants = 200, entryFee = 100m });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var category = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.Equal(ev.EventId, category!.EventId);
        Assert.Equal(5m, category.DistanceKm);
    }

    [Fact]
    public async Task CreateCategory_AsParticipant_Returns403()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");
        var (participant, _) = await CreateUserClientAsync(factory, "Participant");
        var ev = await CreateEventAsync(organiser);

        var response = await participant.PostAsJsonAsync($"/api/events/{ev.EventId}/categories",
            new { categoryName = "5km", distanceKm = 5m, maxParticipants = 100, entryFee = 0m });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_OnAnotherOrganisersEvent_Returns403()
    {
        var (owner, _) = await CreateUserClientAsync(factory, "Organiser");
        var (other, _) = await CreateUserClientAsync(factory, "Organiser");
        var ev = await CreateEventAsync(owner);

        var response = await other.PostAsJsonAsync($"/api/events/{ev.EventId}/categories",
            new { categoryName = "5km", distanceKm = 5m, maxParticipants = 100, entryFee = 0m });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_EventMissing_Returns404()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events/999999/categories",
            new { categoryName = "5km", distanceKm = 5m, maxParticipants = 100, entryFee = 0m });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_InvalidDistance_Returns400()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");
        var ev = await CreateEventAsync(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{ev.EventId}/categories",
            new { categoryName = "Bad", distanceKm = 0m, maxParticipants = 100, entryFee = 0m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCategories_Anonymous_ReturnsCategoriesForEvent()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);

        var response = await factory.CreateClient().GetAsync($"/api/events/{ev.EventId}/categories");
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(categories!, c => c.CategoryId == category.CategoryId);
    }

    [Fact]
    public async Task GetCategories_EventMissing_Returns404()
    {
        var response = await factory.CreateClient().GetAsync("/api/events/999999/categories");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_ByOwner_Returns200()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);

        var response = await organiser.PutAsJsonAsync($"/api/categories/{category.CategoryId}",
            new { categoryName = "Updated 15km", distanceKm = 15m, maxParticipants = 50, entryFee = 175m });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.Equal("Updated 15km", updated!.CategoryName);
    }

    [Fact]
    public async Task UpdateCategory_ByOtherOrganiser_Returns403()
    {
        var (owner, _) = await CreateUserClientAsync(factory, "Organiser");
        var (other, _) = await CreateUserClientAsync(factory, "Organiser");
        var ev = await CreateEventAsync(owner);
        var category = await CreateCategoryAsync(owner, ev.EventId);

        var response = await other.PutAsJsonAsync($"/api/categories/{category.CategoryId}",
            new { categoryName = "Hijack", distanceKm = 1m, maxParticipants = 1, entryFee = 0m });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_ByOwner_Returns204()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);

        var response = await organiser.DeleteAsync($"/api/categories/{category.CategoryId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_AsParticipant_Returns403()
    {
        var (organiser, _) = await CreateUserClientAsync(factory, "Organiser");
        var (participant, _) = await CreateUserClientAsync(factory, "Participant");
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);

        var response = await participant.DeleteAsync($"/api/categories/{category.CategoryId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
