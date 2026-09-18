using System.Text.Json.Serialization;

namespace Aetherphone.Core.Hunts;

internal sealed class HuntMobDescriptionReference
{
    [JsonPropertyName("sheet")]
    public string Sheet { get; set; } = string.Empty;

    [JsonPropertyName("row")]
    public uint Row { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; } = 1;
}

[JsonSerializable(typeof(Dictionary<string, HuntMobDescriptionReference>))]
internal partial class HuntMobDescriptionCatalogJsonContext : JsonSerializerContext
{
}
