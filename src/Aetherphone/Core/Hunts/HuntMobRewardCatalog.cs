using System.Text.Json.Serialization;

namespace Aetherphone.Core.Hunts;

internal sealed class HuntMobRewardCatalog
{
    private readonly HuntJsonCatalogLoader<HuntMobRewardData> loader;
    private Dictionary<string, HuntMobRewardEntry[]> mobRewards = new();
    private Dictionary<string, uint> itemRowIds = new();

    public HuntMobRewardCatalog(FileInfo source)
    {
        loader = new HuntJsonCatalogLoader<HuntMobRewardData>(source,
            HuntMobRewardCatalogJsonContext.Default.HuntMobRewardData, "mob reward catalog", OnLoaded);
        loader.Preload();
    }

    public IReadOnlyList<HuntMobRewardEntry> RewardsFor(string mobId)
    {
        loader.EnsureLoaded();
        return mobRewards.TryGetValue(mobId, out var entries) ? entries : Array.Empty<HuntMobRewardEntry>();
    }

    public uint? ItemRowIdFor(string itemId)
    {
        loader.EnsureLoaded();
        return itemRowIds.TryGetValue(itemId, out var rowId) ? rowId : null;
    }

    private void OnLoaded(HuntMobRewardData parsed)
    {
        itemRowIds = parsed.Items;
        var expanded = new Dictionary<string, HuntMobRewardEntry[]>();
        foreach (var set in parsed.RewardSets)
        {
            foreach (var mobId in set.MobIds)
            {
                expanded[mobId] = set.Rewards;
            }
        }

        mobRewards = expanded;
    }
}

internal sealed class HuntMobRewardData
{
    [JsonPropertyName("items")]
    public Dictionary<string, uint> Items { get; set; } = new();

    [JsonPropertyName("rewardSets")]
    public HuntMobRewardSet[] RewardSets { get; set; } = Array.Empty<HuntMobRewardSet>();
}

internal sealed class HuntMobRewardSet
{
    [JsonPropertyName("mobIds")]
    public string[] MobIds { get; set; } = Array.Empty<string>();

    [JsonPropertyName("rewards")]
    public HuntMobRewardEntry[] Rewards { get; set; } = Array.Empty<HuntMobRewardEntry>();
}

internal sealed class HuntMobRewardEntry
{
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public int? Amount { get; set; }
}

[JsonSerializable(typeof(HuntMobRewardData))]
internal partial class HuntMobRewardCatalogJsonContext : JsonSerializerContext
{
}
