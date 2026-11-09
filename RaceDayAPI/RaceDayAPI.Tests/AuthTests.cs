using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDayAPI.Data;
using static RaceDayAPI.Tests.TestHelpers;

namespace RaceDayAPI.Tests;

/// <summary>User registration, login, password hashing and profile access.</summary>
public class AuthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Register_ValidUser_Returns201_AndNeverReturnsPassword(string role)
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Tina Jeke", email = UniqueEmail(), password = Password, role });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal(role, user!.Role);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        var client = factory.CreateClient();
        var email = UniqueEmail();
        await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Hash Check", email, password = Password, role = "Participant" });

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.Users.SingleAsync(u => u.Email == email);

        Assert.NotEqual(Password, stored.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(Password, stored.PasswordHash));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = factory.CreateClient();
        var email = UniqueEmail();
        var body = new { fullName = "Dup User", email, password = Password, role = "Participant" };

        await client.PostAsJsonAsync("/api/auth/register", body);
        var second = await client.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Bad Role", email = UniqueEmail(), password = Password, role = "Admin" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPasswordAndBadEmail_Returns400()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Bad Input", email = "not-an-email", password = "123", role = "Participant" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenAndUserSummary()
    {
        var client = factory.CreateClient();
        var email = UniqueEmail();
        await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Login User", email, password = Password, role = "Organiser" });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));
        Assert.Equal(email, auth.User.Email);
        Assert.Equal("Organiser", auth.User.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = factory.CreateClient();
        var email = UniqueEmail();
        await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Wrong Pw", email, password = Password, role = "Participant" });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = UniqueEmail("nobody"), password = Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithInvalidToken_Returns401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.real.token");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task GetProfile_AnyRole_ReturnsOwnProfile(string role)
    {
        var (client, user) = await CreateUserClientAsync(factory, role);

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal(user.UserId, me!.UserId);
        Assert.Equal(role, me.Role);
    }

    [Fact]
    public async Task UpdateProfile_ValidData_Returns200_AndPersists()
    {
        var (client, _) = await CreateUserClientAsync(factory, "Participant");

        var update = await client.PutAsJsonAsync("/api/users/me",
            new { fullName = "Updated Name", phoneNumber = "0821234567" });
        var me = await client.GetFromJsonAsync<UserDto>("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Updated Name", me!.FullName);
        Assert.Equal("0821234567", me.PhoneNumber);
    }

    [Fact]
    public async Task UpdateProfile_EmptyName_Returns400()
    {
        var (client, _) = await CreateUserClientAsync(factory, "Participant");

        var response = await client.PutAsJsonAsync("/api/users/me", new { fullName = "", phoneNumber = "0821234567" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
