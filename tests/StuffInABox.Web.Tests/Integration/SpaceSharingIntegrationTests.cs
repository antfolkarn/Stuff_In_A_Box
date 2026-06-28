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

/// <summary>
/// End-to-end checks for the space-sharing security boundary: only the owner and
/// invited members may reach a space's content.
/// </summary>
public class SpaceSharingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly SqliteConnection _keepAlive;

    public SpaceSharingIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _keepAlive = new SqliteConnection("Data Source=sib_sharing_test;Mode=Memory;Cache=Shared");
        _keepAlive.Open();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite("Data Source=sib_sharing_test;Mode=Memory;Cache=Shared"));
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

    [Fact]
    public async Task NonMember_CannotAccess_UntilInviteAccepted()
    {
        // Owner sets up a space with a box.
        var owner = await SignInAsync("owner-share@test.se");
        var sp = await owner.PostAsJsonAsync("/api/v1/spaces", new { name = "Vinden", icon = "ti-home" });
        var spaceId = (await sp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("spaceId").GetString()!;
        var bx = await owner.PostAsJsonAsync("/api/v1/boxes", new { spaceId, label = "Jul" });
        var boxNumber = (await bx.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("boxNumber").GetInt32();

        // Outsider is denied — both the box list and the box detail.
        var member = await SignInAsync("member-share@test.se");
        var deniedList = await member.GetAsync($"/api/v1/boxes/space/{spaceId}");
        Assert.Equal(HttpStatusCode.Forbidden, deniedList.StatusCode);
        var deniedDetail = await member.GetAsync($"/api/v1/boxes/{boxNumber}?spaceId={spaceId}");
        Assert.Equal(HttpStatusCode.Forbidden, deniedDetail.StatusCode);

        // The shared space is not in the outsider's list yet.
        var before = await member.GetFromJsonAsync<JsonElement>("/api/v1/spaces");
        Assert.Equal(0, before.GetArrayLength());

        // Owner creates a share link; member redeems it.
        var inviteResp = await owner.PostAsync($"/api/v1/spaces/{spaceId}/invite", null);
        var token = (await inviteResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        var accept = await member.PostAsync($"/api/v1/invites/{token}/accept", null);
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);

        // Now the member can read the space and its boxes.
        var allowedList = await member.GetAsync($"/api/v1/boxes/space/{spaceId}");
        Assert.Equal(HttpStatusCode.OK, allowedList.StatusCode);
        var boxes = await allowedList.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, boxes.GetArrayLength());

        var spaces = await member.GetFromJsonAsync<JsonElement>("/api/v1/spaces");
        Assert.Equal(1, spaces.GetArrayLength());
        Assert.False(spaces[0].GetProperty("isOwner").GetBoolean());

        // A member may add content; it lands in the owner's space.
        var addItem = await member.PostAsJsonAsync($"/api/v1/boxes/{boxNumber}/items",
            new { spaceId, name = "Grankulor" });
        Assert.Equal(HttpStatusCode.Created, addItem.StatusCode);

        // But a member may NOT manage the space (owner-only).
        var deleteSpace = await member.DeleteAsync($"/api/v1/spaces/{spaceId}");
        Assert.Equal(HttpStatusCode.Forbidden, deleteSpace.StatusCode);
    }

    [Fact]
    public async Task Members_ShowEmailByDefault_AndNicknameWhenSet()
    {
        var owner = await SignInAsync("owner-members@test.se");
        var sp = await owner.PostAsJsonAsync("/api/v1/spaces", new { name = "Garaget", icon = "ti-car" });
        var spaceId = (await sp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("spaceId").GetString()!;

        var inviteResp = await owner.PostAsync($"/api/v1/spaces/{spaceId}/invite", null);
        var token = (await inviteResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();

        var member = await SignInAsync("member-members@test.se");
        await member.PostAsync($"/api/v1/invites/{token}/accept", null);

        // No nickname yet → the owner sees the member's email.
        var listed = await owner.GetFromJsonAsync<JsonElement>($"/api/v1/spaces/{spaceId}/members");
        Assert.Equal(1, listed.GetArrayLength());
        Assert.Equal("member-members@test.se", listed[0].GetProperty("displayName").GetString());

        // Member sets a nickname → that wins over the email.
        var put = await member.PutAsJsonAsync("/api/v1/settings",
            new { theme = "system", design = "standard", displayName = "Stina" });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var relisted = await owner.GetFromJsonAsync<JsonElement>($"/api/v1/spaces/{spaceId}/members");
        Assert.Equal("Stina", relisted[0].GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task RevokedInvite_CannotBeRedeemed()
    {
        var owner = await SignInAsync("owner-revoke@test.se");
        var sp = await owner.PostAsJsonAsync("/api/v1/spaces", new { name = "Förråd", icon = "ti-box" });
        var spaceId = (await sp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("spaceId").GetString()!;

        var inviteResp = await owner.PostAsync($"/api/v1/spaces/{spaceId}/invite", null);
        var token = (await inviteResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        await owner.DeleteAsync($"/api/v1/spaces/{spaceId}/invite");

        var member = await SignInAsync("member-revoke@test.se");
        var accept = await member.PostAsync($"/api/v1/invites/{token}/accept", null);
        Assert.Equal(HttpStatusCode.NotFound, accept.StatusCode);
    }
}
