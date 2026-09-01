using Newtonsoft.Json;
using Xunit;

namespace Aetherphone.Tests;

public sealed class ConfigurationBadgeMigrationTests
{
    [Fact]
    public void LegacyBadgeFlagsDeserializeFromTheirPreRenameJsonKeys()
    {
        const string json = """{"ShowWalletBadge":false,"ShowDailiesBadge":false,"ShowActivityBadge":true}""";

        var configuration = JsonConvert.DeserializeObject<Configuration>(json);

        Assert.NotNull(configuration);
        Assert.False(configuration!.LegacyShowWalletBadge);
        Assert.False(configuration.LegacyShowDailiesBadge);
        Assert.True(configuration.LegacyShowActivityBadge);
    }

    [Fact]
    public void LegacyBadgeFlagsDefaultToTrueWhenAbsentFromSavedJson()
    {
        const string json = "{}";

        var configuration = JsonConvert.DeserializeObject<Configuration>(json);

        Assert.NotNull(configuration);
        Assert.True(configuration!.LegacyShowWalletBadge);
        Assert.True(configuration.LegacyShowDailiesBadge);
        Assert.True(configuration.LegacyShowActivityBadge);
    }
}
