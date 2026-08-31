namespace Aetherphone.Core.Hunts;

internal sealed class HuntCandidateCache
{
    private readonly HuntMobCatalog mobCatalog;
    private readonly HuntZoneCatalog zoneCatalog;
    private readonly HuntsService hunts;
    private readonly Dictionary<(string MobId, string WorldId, int ZoneInstance, string ZoneId), Entry> entries = new();

    private readonly struct Entry
    {
        public readonly HuntCandidateStateToken Token;
        public readonly List<HuntPoiState> States;
        public readonly int? ReportedPoiId;

        public Entry(HuntCandidateStateToken token, List<HuntPoiState> states, int? reportedPoiId)
        {
            Token = token;
            States = states;
            ReportedPoiId = reportedPoiId;
        }
    }

    public HuntCandidateCache(HuntMobCatalog mobCatalog, HuntZoneCatalog zoneCatalog, HuntsService hunts)
    {
        this.mobCatalog = mobCatalog;
        this.zoneCatalog = zoneCatalog;
        this.hunts = hunts;
    }

    public (IReadOnlyList<HuntPoiState> States, int? ReportedPoiId) ResolveFor(HuntMobDefinition mob,
        string worldId, int zoneInstance, string zoneId, bool includeLandmineOnlySpots)
    {
        var token = hunts.CandidateStateToken;
        var key = (mob.Id, worldId, zoneInstance, zoneId);
        if (!entries.TryGetValue(key, out var entry) || !IsFresh(entry.Token, token, mob.Id, worldId, zoneInstance, zoneId))
        {
            var states = new List<HuntPoiState>();
            HuntCandidateResolver.ResolveMobZoneStates(mob, worldId, zoneInstance, zoneId, mobCatalog, zoneCatalog,
                hunts, states, out var reportedPoiId);
            entry = new Entry(token, states, reportedPoiId);
            entries[key] = entry;
        }

        if (!includeLandmineOnlySpots)
        {
            return (entry.States, entry.ReportedPoiId);
        }

        var withLandmines = new List<HuntPoiState>(entry.States);
        HuntCandidateResolver.AppendLandmineOnlyStates(mob, zoneId, mobCatalog, zoneCatalog, withLandmines);
        return (withLandmines, entry.ReportedPoiId);
    }

    private bool IsFresh(HuntCandidateStateToken cached, HuntCandidateStateToken current, string mobId,
        string worldId, int zoneInstance, string zoneId)
    {
        if (cached.ActiveSpawnVersion != current.ActiveSpawnVersion)
        {
            return false;
        }

        var spawnKey = new HuntSpawnKey(mobId, worldId, zoneInstance);
        return ValueEqualFor(cached.Locations, current.Locations, spawnKey) &&
            ValueEqualFor(cached.Phases, current.Phases, spawnKey) &&
            ValueEqualFor(cached.Zones, current.Zones, spawnKey) &&
            SightingsEqualForZone(cached.SightedPoiIds, current.SightedPoiIds, mobId, worldId, zoneInstance, zoneId);
    }

    private static bool ValueEqualFor<TValue>(Dictionary<HuntSpawnKey, TValue> cached,
        Dictionary<HuntSpawnKey, TValue> current, HuntSpawnKey key)
    {
        if (ReferenceEquals(cached, current))
        {
            return true;
        }

        var hasCached = cached.TryGetValue(key, out var cachedValue);
        var hasCurrent = current.TryGetValue(key, out var currentValue);
        if (hasCached != hasCurrent)
        {
            return false;
        }

        return !hasCached || EqualityComparer<TValue>.Default.Equals(cachedValue, currentValue);
    }

    private bool SightingsEqualForZone(HashSet<HuntSightingKey> cached, HashSet<HuntSightingKey> current,
        string mobId, string worldId, int zoneInstance, string zoneId)
    {
        if (ReferenceEquals(cached, current))
        {
            return true;
        }

        var zone = zoneCatalog.FindZone(zoneId);
        if (zone is null)
        {
            return true;
        }

        var pois = zone.Pois;
        for (var index = 0; index < pois.Length; index++)
        {
            var sightingKey = new HuntSightingKey(mobId, worldId, zoneInstance, pois[index].Id);
            if (cached.Contains(sightingKey) != current.Contains(sightingKey))
            {
                return false;
            }
        }

        return true;
    }
}
