using Aetherphone.Core.Hunts;
using Xunit;

namespace Aetherphone.Tests;

public sealed class HuntMobDescriptionCatalogTests
{
    private static FileInfo Source() =>
        new(Path.Combine(AppContext.BaseDirectory, "Hunts", "HuntMobDescriptions.json"));

    [Fact]
    public void ResolvesAKnownMobToItsSheetReference()
    {
        var catalog = new HuntMobDescriptionCatalog(Source());

        var reference = catalog.ById["aegeiros"];

        Assert.Equal("custom/007/CtsHnt60RiskyMobThavnair_00762", reference.Sheet);
        Assert.Equal(42u, reference.Row);
        Assert.Equal(3, reference.Count);
    }

    [Fact]
    public void FateLinkedMobsAreNotInTheCatalogAnymore()
    {
        var catalog = new HuntMobDescriptionCatalog(Source());

        Assert.False(catalog.ById.ContainsKey("behemoth"));
    }

    [Fact]
    public void EveryEntryHasANonEmptySheetName()
    {
        var catalog = new HuntMobDescriptionCatalog(Source());

        Assert.NotEmpty(catalog.ById);
        Assert.All(catalog.ById.Values, reference => Assert.False(string.IsNullOrWhiteSpace(reference.Sheet)));
    }

    [Fact]
    public void MissingSourceFileDoesNotThrow()
    {
        var missing = new FileInfo(Path.Combine(AppContext.BaseDirectory, "Hunts", "DoesNotExist.json"));
        var catalog = new HuntMobDescriptionCatalog(missing);

        Assert.Empty(catalog.ById);
        Assert.Null(catalog.DescriptionFor("behemoth", def: null));
    }
}
