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
        if (!entries.TryGetValue(key, out var entry) || !entry.Token.Equals(token))
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
}
