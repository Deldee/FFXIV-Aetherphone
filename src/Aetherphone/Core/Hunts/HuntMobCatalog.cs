namespace Aetherphone.Core.Hunts;

internal sealed class HuntMobCatalog
{
    private readonly HuntJsonCatalogLoader<Dictionary<string, HuntMobDefinition>> loader;
    private readonly Dictionary<string, HashSet<int>> ssRankPoiIdsByZone = new();
    private Dictionary<string, HuntMobDefinition> byId = new();
    private HashSet<int>? landminePoiIds;

    public HuntMobCatalog(FileInfo source)
    {
        loader = new HuntJsonCatalogLoader<Dictionary<string, HuntMobDefinition>>(source,
            HuntMobCatalogJsonContext.Default.DictionaryStringHuntMobDefinition, "mob catalog",
            parsed => byId = parsed);
        loader.Preload();
    }

    public IReadOnlyDictionary<string, HuntMobDefinition> ById
    {
        get
        {
            loader.EnsureLoaded();
            return byId;
        }
    }

    public HuntMobDefinition? Find(string mobId)
    {
        loader.EnsureLoaded();
        return byId.GetValueOrDefault(mobId);
    }

    public HashSet<int> SsRankPoiIdsForZone(string zoneId)
    {
        loader.EnsureLoaded();
        if (ssRankPoiIdsByZone.TryGetValue(zoneId, out var cached))
        {
            return cached;
        }

        var result = new HashSet<int>();
        foreach (var mob in byId.Values)
        {
            if (mob.Rank != "SS" || Array.IndexOf(mob.ZoneIds, zoneId) < 0)
            {
                continue;
            }

            for (var windowIndex = 0; windowIndex < mob.Windows.Length; windowIndex++)
            {
                var phases = mob.Windows[windowIndex].Phases;
                for (var phaseIndex = 0; phaseIndex < phases.Length; phaseIndex++)
                {
                    var zonePoiIds = phases[phaseIndex].ZonePoiIds;
                    for (var poiIndex = 0; poiIndex < zonePoiIds.Length; poiIndex++)
                    {
                        result.Add(zonePoiIds[poiIndex]);
                    }
                }
            }
        }

        ssRankPoiIdsByZone[zoneId] = result;
        return result;
    }

    public bool IsLandminePoi(int zonePoiId) => (landminePoiIds ??= BuildLandminePoiIds()).Contains(zonePoiId);

    private HashSet<int> BuildLandminePoiIds() => BuildPoiIdsForRanks(static rank => rank is "A" or "B");

    private HashSet<int> BuildPoiIdsForRanks(Func<string, bool> matchesRank)
    {
        var result = new HashSet<int>();
        foreach (var mob in ById.Values)
        {
            if (!matchesRank(mob.Rank))
            {
                continue;
            }

            for (var windowIndex = 0; windowIndex < mob.Windows.Length; windowIndex++)
            {
                var phases = mob.Windows[windowIndex].Phases;
                for (var phaseIndex = 0; phaseIndex < phases.Length; phaseIndex++)
                {
                    var zonePoiIds = phases[phaseIndex].ZonePoiIds;
                    for (var poiIndex = 0; poiIndex < zonePoiIds.Length; poiIndex++)
                    {
                        result.Add(zonePoiIds[poiIndex]);
                    }
                }
            }
        }

        return result;
    }
}
