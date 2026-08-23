namespace Aetherphone.Core.Social;

[Serializable]
internal sealed class VelvetFilterPreferences
{
    public int Intent { get; set; }
    public int Gender { get; set; }
    public int Sexuality { get; set; }
    public int Relationship { get; set; }
    public string Region { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public List<string> Kinks { get; set; } = new();
    public List<string> Limits { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}
