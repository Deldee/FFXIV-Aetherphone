using Aetherphone.Core.Hunts;
using Xunit;

namespace Aetherphone.Tests;

public sealed class HuntZoneCatalogTests
{
    private static FileInfo Source() => new(Path.Combine(AppContext.BaseDirectory, "Hunts", "HuntPOI.json"));

    [Fact]
    public void ResolvesTheTerritoryIdStoredForAZone()
    {
        var catalog = new HuntZoneCatalog(Source());

        var territoryId = catalog.ResolveTerritoryId("middle_la_noscea");

        Assert.Equal(134u, territoryId);
    }

    [Fact]
    public void ReturnsZeroForAnUnknownZone()
    {
        var catalog = new HuntZoneCatalog(Source());

        var territoryId = catalog.ResolveTerritoryId("not_a_real_zone_id");

        Assert.Equal(0u, territoryId);
    }

    [Fact]
    public void ZoneIdForTerritoryRoundTripsResolveTerritoryId()
    {
        var catalog = new HuntZoneCatalog(Source());

        var zoneId = catalog.ZoneIdForTerritory(134u);

        Assert.Equal("middle_la_noscea", zoneId);
    }

    [Fact]
    public void ResolvesAKnownMobSpawnPoi()
    {
        var catalog = new HuntZoneCatalog(Source());

        var found = catalog.FindPoi(6);

        Assert.NotNull(found);
        Assert.Equal("middle_la_noscea", found!.Value.ZoneId);
    }

    [Fact]
    public void AetherytesAreNoLongerStoredAsPois()
    {
        var catalog = new HuntZoneCatalog(Source());

        var found = catalog.FindPoi(3);

        Assert.Null(found);
    }
}
