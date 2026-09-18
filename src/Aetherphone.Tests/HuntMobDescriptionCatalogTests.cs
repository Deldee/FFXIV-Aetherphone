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

        var reference = catalog.ById["behemoth"];

        Assert.Equal("Fate", reference.Sheet);
        Assert.Equal(505u, reference.Row);
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
        Assert.Null(catalog.DescriptionFor("behemoth"));
    }
}
