using Microsoft.Extensions.Configuration;
using StuffInABox.Infrastructure.Admin;

namespace StuffInABox.Infrastructure.Tests.Admin;

public class PlanCatalogTests
{
    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void NoConfig_UsesBuiltInDefaults()
    {
        var catalog = new PlanCatalog(Config([]));

        Assert.Equal(["free", "medium", "large"], catalog.Tiers);
        Assert.True(catalog.IsValidTier("FREE")); // case-insensitive
        Assert.False(catalog.IsValidTier("enterprise"));

        var free = catalog.GetPlan("free");
        Assert.NotNull(free);
        Assert.Equal(0, free!.PriceSek);
        Assert.Equal(20, free.AiPhotosPerMonth);

        Assert.Equal(-1, catalog.GetPlan("large")!.MaxItems); // unlimited
    }

    [Fact]
    public void Config_OverridesCatalogAndPreservesOrder()
    {
        var catalog = new PlanCatalog(Config(new Dictionary<string, string?>
        {
            ["Plans:starter:priceSek"] = "0",
            ["Plans:starter:maxSpaces"] = "2",
            ["Plans:pro:priceSek"] = "129",
            ["Plans:pro:maxSpaces"] = "-1",
            ["Plans:pro:claudeEnrichment"] = "true",
        }));

        Assert.Equal(["starter", "pro"], catalog.Tiers);
        Assert.Equal(2, catalog.GetPlan("starter")!.MaxSpaces);
        Assert.Equal(129, catalog.GetPlan("pro")!.PriceSek);
        Assert.True(catalog.GetPlan("pro")!.ClaudeEnrichment);
        Assert.False(catalog.IsValidTier("free")); // defaults are replaced, not merged
    }
}
