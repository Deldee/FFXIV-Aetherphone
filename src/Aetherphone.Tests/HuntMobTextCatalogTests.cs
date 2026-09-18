using Aetherphone.Core.Hunts;
using Xunit;

namespace Aetherphone.Tests;

public sealed class HuntMobTextCatalogTests
{
    private static FileInfo TipsSource() =>
        new(Path.Combine(AppContext.BaseDirectory, "Hunts", "HuntMobTips.json"));

    [Fact]
    public void ResolvesTipsFromTheirOwnFile()
    {
        var catalog = new HuntMobTextCatalog(TipsSource());

        var text = catalog.TextFor("ker", "de");

        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.Null(catalog.TextFor("forneus", "en"));
    }

    [Fact]
    public void ReturnsNullForAMobIdTheFileDoesNotHave()
    {
        var catalog = new HuntMobTextCatalog(TipsSource());

        var text = catalog.TextFor("not_a_real_mob_id", "en");

        Assert.Null(text);
    }

    [Fact]
    public void MissingSourceFileDoesNotThrow()
    {
        var missing = new FileInfo(Path.Combine(AppContext.BaseDirectory, "Hunts", "DoesNotExist.json"));
        var catalog = new HuntMobTextCatalog(missing);

        var text = catalog.TextFor("ker", "en");

        Assert.Null(text);
    }
}
