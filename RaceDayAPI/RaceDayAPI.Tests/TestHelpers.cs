namespace RaceDayAPI.Tests;

public static class TestHelpers
{
    public const string Password = "Passw0rd!123";

    public static string UniqueEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@test.com";

    /// <summary>Registers a new user with the given role, logs in, and returns a client carrying their JWT.</summary>
    public static async Task<(HttpClient Client, UserDto User)> CreateUserClientAsync(ApiFactory factory, string role)
    {
        var anonymous = factory.CreateClient();
        var email = UniqueEmail(role.ToLowerInvariant());

        var register = await anonymous.PostAsJsonAsync("/api/auth/register",
            new { fullName = $"Test {role}", email, password = Password, role });
        register.EnsureSuccessStatusCode();

        var login = await anonymous.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();
        var auth = (await login.Content.ReadFromJsonAsync<AuthResponse>())!;

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return (client, auth.User);
    }

    public static async Task<EventDto> CreateEventAsync(HttpClient organiser, string name = "Test City Run")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventName = name,
            eventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            location = "Johannesburg, Gauteng",
            description = "Created by a unit test"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDto>())!;
    }

    public static async Task<CategoryDto> CreateCategoryAsync(HttpClient organiser, int eventId, int maxParticipants = 100)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "10km Race",
            distanceKm = 10m,
            maxParticipants,
            entryFee = 150m
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    public static async Task<EnrolmentDto> EnrolAsync(HttpClient participant, int categoryId)
    {
        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EnrolmentDto>())!;
    }
}
