using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StuffInABox.Infrastructure.Persistence;

namespace StuffInABox.Web.Tests.Integration;

public class PhotoUploadIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    // A 4x4 PNG encoded by SkiaSharp (guaranteed decodable by the server's processor)
    private const string Png1x1Base64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAQAAAAECAYAAACp8Z5+AAAABHNCSVQICAgIfAhkiAAAABVJREFUCJljTJn69j8DEmBiQAOEBQC91wLtDhuSxgAAAABJRU5ErkJggg==";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly SqliteConnection _keepAlive;

    public PhotoUploadIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _keepAlive = new SqliteConnection("Data Source=sib_photo_test;Mode=Memory;Cache=Shared");
        _keepAlive.Open();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite("Data Source=sib_photo_test;Mode=Memory;Cache=Shared"));
            });
            builder.UseSetting("Jwt:Secret", "test-secret-key-min-32-chars-long!!");
            builder.UseSetting("RateLimiting:AuthPermitLimit", "1000");
        });
    }

    public void Dispose() => _keepAlive.Dispose();

    private async Task<(HttpClient client, string spaceId, int boxNumber, string itemId)> SetupItemAsync()
    {
        var client = _factory.CreateClient();
        var reg = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "photo-int@test.se", password = "password123" });
        var token = (await reg.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var sp = await client.PostAsJsonAsync("/api/v1/spaces", new { name = "Garage", icon = "ti-car" });
        var spaceId = (await sp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("spaceId").GetString();

        var bx = await client.PostAsJsonAsync("/api/v1/boxes", new { spaceId, label = "Tools" });
        var boxNumber = (await bx.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("boxNumber").GetInt32();

        var it = await client.PostAsJsonAsync($"/api/v1/boxes/{boxNumber}/items", new { spaceId, name = "Hammer" });
        var itemId = (await it.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("itemId").GetString();

        return (client, spaceId!, boxNumber, itemId!);
    }

    [Fact]
    public async Task UploadPhoto_ValidPng_Returns200WithUrl()
    {
        var (client, spaceId, boxNumber, itemId) = await SetupItemAsync();

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(Convert.FromBase64String(Png1x1Base64));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "photo.png");

        var resp = await client.PostAsync($"/api/v1/boxes/{boxNumber}/items/{itemId}/photo", content);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var url = body.GetProperty("photoUrl").GetString();
        Assert.StartsWith("/uploads/", url);

        // The item now reports a photo URL
        var items = await client.GetFromJsonAsync<JsonElement>($"/api/v1/boxes/{boxNumber}/items?spaceId={spaceId}");
        Assert.False(string.IsNullOrEmpty(items[0].GetProperty("photoUrl").GetString()));

        // And the photo is actually served (regression guard: uploads must live
        // outside wwwroot so they survive SPA rebuilds)
        var photo = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, photo.StatusCode);
        Assert.StartsWith("image/", photo.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ServePhoto_WithoutSignature_Returns403()
    {
        var (client, _, boxNumber, itemId) = await SetupItemAsync();

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(Convert.FromBase64String(Png1x1Base64));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "photo.png");
        var resp = await client.PostAsync($"/api/v1/boxes/{boxNumber}/items/{itemId}/photo", content);
        var url = (await resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("photoUrl").GetString()!;

        // Strip the signature — the bare /uploads/{key} must not be world-readable.
        var keyPath = url.Split('?')[0];
        var unsigned = await client.GetAsync(keyPath);
        Assert.Equal(HttpStatusCode.Forbidden, unsigned.StatusCode);

        // A tampered signature is rejected too.
        var tampered = await client.GetAsync($"{keyPath}?sig=99999999999.deadbeef");
        Assert.Equal(HttpStatusCode.Forbidden, tampered.StatusCode);
    }

    [Fact]
    public async Task UploadPhoto_NonImage_Returns400()
    {
        var (client, _, boxNumber, itemId) = await SetupItemAsync();

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes("this is not an image"));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "fake.png");

        var resp = await client.PostAsync($"/api/v1/boxes/{boxNumber}/items/{itemId}/photo", content);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
