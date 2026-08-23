using Aetherphone.Core;
using Aetherphone.Core.Social;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace Aetherphone.Apps.Velvet;

internal sealed class StoredVelvetFilters
{
    public VelvetFilterPreferences DiscoverInclude { get; set; } = new();
    public VelvetFilterPreferences DiscoverExclude { get; set; } = new();
    public VelvetFilterPreferences FeedInclude { get; set; } = new();
    public VelvetFilterPreferences FeedExclude { get; set; } = new();
}

internal sealed class VelvetFilterArchive
{
    private readonly object sync = new();
    private readonly DirectoryInfo baseDir;

    public VelvetFilterArchive(DirectoryInfo baseDir)
    {
        this.baseDir = baseDir;
        if (!baseDir.Exists)
        {
            baseDir.Create();
        }
    }

    public StoredVelvetFilters Load(string accountId)
    {
        if (accountId.Length == 0)
        {
            return new StoredVelvetFilters();
        }

        try
        {
            var path = PathFor(accountId);
            if (!File.Exists(path))
            {
                return new StoredVelvetFilters();
            }

            var stored = JsonConvert.DeserializeObject<StoredVelvetFilters>(File.ReadAllText(path));
            return new StoredVelvetFilters
            {
                DiscoverInclude = stored?.DiscoverInclude ?? new VelvetFilterPreferences(),
                DiscoverExclude = stored?.DiscoverExclude ?? new VelvetFilterPreferences(),
                FeedInclude = stored?.FeedInclude ?? new VelvetFilterPreferences(),
                FeedExclude = stored?.FeedExclude ?? new VelvetFilterPreferences(),
            };
        }
        catch (Exception exception)
        {
            AepLog.Warning(exception, $"VelvetFilterArchive load failed for {accountId}");
            return new StoredVelvetFilters();
        }
    }

    public bool Save(string accountId, StoredVelvetFilters filters)
    {
        if (accountId.Length == 0)
        {
            return false;
        }

        try
        {
            lock (sync)
            {
                var path = PathFor(accountId);
                var temp = path + ".tmp";
                File.WriteAllText(temp, JsonConvert.SerializeObject(filters));
                File.Move(temp, path, true);
            }

            return true;
        }
        catch (Exception exception)
        {
            AepLog.Warning(exception, $"VelvetFilterArchive write failed for {accountId}");
            return false;
        }
    }

    private string PathFor(string accountId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(accountId.ToLowerInvariant()));
        var builder = new StringBuilder(hash.Length * 2 + 5);
        for (var index = 0; index < hash.Length; index++)
        {
            builder.Append(hash[index].ToString("x2"));
        }

        builder.Append(".json");
        return Path.Combine(baseDir.FullName, builder.ToString());
    }
}
