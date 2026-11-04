using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Security.Dtos;

namespace Security.Tests;

public class UsersApiTests
{
    private readonly SqliteConnection _keepDatabaseAlive;
    private readonly HttpClient _client;

    public UsersApiTests()
    {
        // A database in memory that stays alive while this connection is open.
        var connectionString = $"Data Source={Guid.NewGuid()};Mode=Memory;Cache=Shared";
        _keepDatabaseAlive = new SqliteConnection(connectionString);
        _keepDatabaseAlive.Open();

        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
            builder.UseSetting("JwtSecret", "a-secret-that-is-only-used-in-these-tests");
            builder.UseSetting("AdminUsername", "boss");
            builder.UseSetting("AdminPassword", "Secret123");
        });
        _client = factory.CreateClient();
    }

    private record TokenResponse(string Token);

    private async Task LoginAs(string username, string password = "Secret123")
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync("/login", new LoginRequest(username, password));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
    }

    // Logs in as the admin and creates a user. Stays logged in as the admin.
    private async Task<UserResponse> CreateAsAdmin(string username, string role)
    {
        await LoginAs("boss");
        var response = await Create(username, role);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserResponse>())!;
    }

    private Task<HttpResponseMessage> Create(string username, string role) =>
        _client.PostAsJsonAsync("/api/users", new CreateUserRequest(username, "Secret123", role));

    private Task<HttpResponseMessage> Update(int id, string username, string role, string? password = null) =>
        _client.PutAsJsonAsync($"/api/users/{id}", new UpdateUserRequest(username, password, role));

    private Task<HttpResponseMessage> Delete(int id) =>
        _client.DeleteAsync($"/api/users/{id}");

    [Fact]
    public async Task NoToken_Gives401()
    {
        var response = await _client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsTheLoggedInUser()
    {
        await CreateAsAdmin("alice", "user");
        await LoginAs("alice");

        var me = await _client.GetFromJsonAsync<UserResponse>("/api/users/me");

        Assert.Equal("alice", me?.Username);
        Assert.Equal("user", me?.Role);
    }
    
    [Fact]
    public async Task User_CanRead_ButNotChange()
    {
        await CreateAsAdmin("alice", "user");
        var bob = await CreateAsAdmin("bob", "user");
        await LoginAs("alice");

        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Create("carl", "user")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Update(bob.Id, "bobby", "user")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Delete(bob.Id)).StatusCode);
    }

    [Fact]
    public async Task Superuser_ChangesUsers()
    {
        var bob = await CreateAsAdmin("bob", "user");
        await CreateAsAdmin("sam", "superuser");
        await LoginAs("sam");

        Assert.Equal(HttpStatusCode.Created, (await Create("carl", "user")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Update(bob.Id, "bobby", "user")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Delete(bob.Id)).StatusCode);
    }

    [Fact]
    public async Task Superuser_CannotChangeHigherRoles()
    {
        var other = await CreateAsAdmin("other", "superuser");
        await CreateAsAdmin("sam", "superuser");
        await LoginAs("sam");

        Assert.Equal(HttpStatusCode.Forbidden, (await Create("carl", "admin")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Update(other.Id, "other2", "superuser")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Delete(other.Id)).StatusCode);
    }

    [Fact]
    public async Task Superuser_CannotGiveAHigherRole()
    {
        var bob = await CreateAsAdmin("bob", "user");
        await CreateAsAdmin("sam", "superuser");
        await LoginAs("sam");

        Assert.Equal(HttpStatusCode.Forbidden, (await Update(bob.Id, "bob", "admin")).StatusCode);
    }

    [Fact]
    public async Task Admin_ChangesEveryone()
    {
        var other = await CreateAsAdmin("other", "admin");

        Assert.Equal(HttpStatusCode.OK, (await Update(other.Id, "other2", "superuser")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Delete(other.Id)).StatusCode);
    }

    [Fact]
    public async Task Admin_CannotDeleteThemselves()
    {
        await LoginAs("boss");

        Assert.Equal(HttpStatusCode.Forbidden, (await Delete(1)).StatusCode);
    }

    [Fact]
    public async Task Admin_CannotChangeTheirOwnRole()
    {
        await LoginAs("boss");

        Assert.Equal(HttpStatusCode.Forbidden, (await Update(1, "boss", "user")).StatusCode);
    }

    [Fact]
    public async Task Responses_NeverIncludeThePassword()
    {
        await LoginAs("boss");

        var created = await (await Create("alice", "user")).Content.ReadAsStringAsync();
        var list = await (await _client.GetAsync("/api/users")).Content.ReadAsStringAsync();

        Assert.DoesNotContain("password", created, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", list, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TakenUsername_Gives409()
    {
        var bob = await CreateAsAdmin("bob", "user");

        Assert.Equal(HttpStatusCode.Conflict, (await Create("bob", "user")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Update(bob.Id, "boss", "user")).StatusCode);
    }

    [Fact]
    public async Task UnknownRole_Gives400()
    {
        await LoginAs("boss");

        Assert.Equal(HttpStatusCode.BadRequest, (await Create("alice", "king")).StatusCode);
    }

    [Fact]
    public async Task NewPassword_WorksForLogin()
    {
        var bob = await CreateAsAdmin("bob", "user");

        await Update(bob.Id, "bob", "user", "NewSecret456");

        await LoginAs("bob", "NewSecret456");
    }

    [Fact]
    public async Task LoweredRole_CountsRightAway()
    {
        var sam = await CreateAsAdmin("sam", "superuser");
        await LoginAs("sam");
        var samsOldToken = _client.DefaultRequestHeaders.Authorization;
        await LoginAs("boss");
        await Update(sam.Id, "sam", "user");

        _client.DefaultRequestHeaders.Authorization = samsOldToken;

        Assert.Equal(HttpStatusCode.Forbidden, (await Create("carl", "user")).StatusCode);
    }
}
