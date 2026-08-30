namespace Aetherphone.Core.Hunts;

internal sealed class HuntMobCatalog
{
    private readonly HuntJsonCatalogLoader<Dictionary<string, HuntMobDefinition>> loader;
    private Dictionary<string, HuntMobDefinition> byId = new();
    private HashSet<int>? ssRankPoiIds;
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

    public bool IsSsRankPoi(int zonePoiId) => (ssRankPoiIds ??= BuildSsRankPoiIds()).Contains(zonePoiId);

    public bool IsLandminePoi(int zonePoiId) => (landminePoiIds ??= BuildLandminePoiIds()).Contains(zonePoiId);

    private HashSet<int> BuildSsRankPoiIds() => BuildPoiIdsForRanks(static rank => rank == "SS");

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
