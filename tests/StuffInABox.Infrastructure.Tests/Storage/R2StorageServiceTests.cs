using Microsoft.Extensions.Configuration;
using StuffInABox.Infrastructure.Storage;

namespace StuffInABox.Infrastructure.Tests.Storage;

public class R2StorageServiceTests
{
    private static R2StorageService Build() =>
        new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Storage:R2:AccountId"] = "acc123",
            ["Storage:R2:AccessKey"] = "AKIAEXAMPLE",
            ["Storage:R2:SecretKey"] = "secretexamplekey",
            ["Storage:R2:Bucket"] = "stuffinabox",
            ["Storage:UrlValidityMinutes"] = "60",
        }).Build());

    [Fact]
    public void GetUrl_ProducesPresignedR2Url_ForTheKey()
    {
        var url = Build().GetUrl("abc123.jpg");

        // Points at the configured R2 endpoint + bucket + key…
        Assert.Contains("acc123.r2.cloudflarestorage.com", url);
        Assert.Contains("stuffinabox", url);
        Assert.Contains("abc123.jpg", url);
        // …and is genuinely presigned (SigV4 query parameters present).
        Assert.Contains("X-Amz-Signature=", url);
        Assert.Contains("X-Amz-Expires=", url);
    }

    [Fact]
    public void Constructor_Throws_WhenBucketMissing()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Storage:R2:AccountId"] = "acc123",
            ["Storage:R2:AccessKey"] = "k",
            ["Storage:R2:SecretKey"] = "s",
        }).Build();

        Assert.Throws<InvalidOperationException>(() => new R2StorageService(config));
    }
}
