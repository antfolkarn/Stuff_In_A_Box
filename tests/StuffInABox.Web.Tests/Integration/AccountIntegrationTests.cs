using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StuffInABox.Infrastructure.Persistence;

namespace StuffInABox.Web.Tests.Integration;

/// <summary>GDPR endpoints: data export (portability) and account deletion (erasure).</summary>
public class AccountIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly SqliteConnection _keepAlive;

    public AccountIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _keepAlive = new SqliteConnection("Data Source=sib_account_test;Mode=Memory;Cache=Shared");
        _keepAlive.Open();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite("Data Source=sib_account_test;Mode=Memory;Cache=Shared"));
            });
            builder.UseSetting("Jwt:Secret", "test-secret-key-min-32-chars-long!!");
            builder.UseSetting("RateLimiting:AuthPermitLimit", "1000");
        });
    }

    public void Dispose() => _keepAlive.Dispose();

    private async Task<HttpClient> SignInAsync(string email)
    {
        var client = _factory.CreateClient();
        var reg = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = "password123" });
        TestVerify.MarkVerified(_factory, email);
        var token = (await reg.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<(string spaceId, int boxNumber)> SeedSpaceWithItemAsync(HttpClient client)
    {
        var sp = await client.PostAsJsonAsync("/api/v1/spaces", new { name = "Vinden", icon = "ti-home" });
        var spaceId = (await sp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("spaceId").GetString()!;
        var bx = await client.PostAsJsonAsync("/api/v1/boxes", new { spaceId, label = "Jul" });
        var boxNumber = (await bx.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("boxNumber").GetInt32();
        await client.PostAsJsonAsync($"/api/v1/boxes/{boxNumber}/items", new { spaceId, name = "Grankulor" });
        return (spaceId, boxNumber);
    }

    [Fact]
    public async Task Export_ReturnsOwnedData()
    {
        var client = await SignInAsync("export@test.se");
        await SeedSpaceWithItemAsync(client);

        var resp = await client.GetAsync("/api/v1/account/export");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var export = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("export@test.se", export.GetProperty("account").GetProperty("email").GetString());
        var spaces = export.GetProperty("spaces");
        Assert.Equal(1, spaces.GetArrayLength());
        Assert.Equal("Vinden", spaces[0].GetProperty("name").GetString());
        Assert.Equal("Grankulor", spaces[0].GetProperty("boxes")[0].GetProperty("items")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task DeleteAccount_RemovesData_AndRevokesLogin()
    {
        var client = await SignInAsync("erase@test.se");
        await SeedSpaceWithItemAsync(client);

        var del = await client.DeleteAsync("/api/v1/account");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        // The (still-valid) access token now sees no data.
        var spaces = await client.GetFromJsonAsync<JsonElement>("/api/v1/spaces");
        Assert.Equal(0, spaces.GetArrayLength());

        // The account is gone — the credentials no longer work.
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "erase@test.se", password = "password123" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_RemovesMembersAccessToSharedSpaces()
    {
        var owner = await SignInAsync("owner-erase@test.se");
        TestVerify.SetPlan(_factory, "owner-erase@test.se", "large"); // sharing needs a paid tier
        var (spaceId, _) = await SeedSpaceWithItemAsync(owner);
        var inviteResp = await owner.PostAsync($"/api/v1/spaces/{spaceId}/invite", null);
        var inviteToken = (await inviteResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();

        var member = await SignInAsync("member-erase@test.se");
        await member.PostAsync($"/api/v1/invites/{inviteToken}/accept", null);
        var before = await member.GetAsync($"/api/v1/boxes/space/{spaceId}");
        Assert.Equal(HttpStatusCode.OK, before.StatusCode); // member has access

        await owner.DeleteAsync("/api/v1/account");

        // The space is gone, so the member can no longer reach it and no longer lists it.
        var after = await member.GetAsync($"/api/v1/boxes/space/{spaceId}");
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
        var memberSpaces = await member.GetFromJsonAsync<JsonElement>("/api/v1/spaces");
        Assert.Equal(0, memberSpaces.GetArrayLength());
    }
}
