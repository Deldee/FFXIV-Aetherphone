namespace Aetherphone.Core.Hunts;

internal static class HuntCandidateResolver
{
    public static void ResolveZoneMarkers(string zoneId, string worldId, HuntMobCatalog mobCatalog,
        HuntZoneCatalog zoneCatalog, HuntsService hunts, List<HuntsMapMarkerPoint> results)
    {
        results.Clear();
        if (worldId.Length == 0)
        {
            return;
        }

        var windows = hunts.Windows;
        for (var index = 0; index < windows.Length; index++)
        {
            var window = windows[index];
            if (!string.Equals(window.WorldId, worldId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var mob = mobCatalog.Find(window.MobId);
            if (mob is null || mob.ZoneIds.Length == 0)
            {
                continue;
            }

            var confirmedZoneId = hunts.ZoneIdFor(mob.Id, window.WorldId, window.ZoneInstance);
            if (confirmedZoneId is { Length: > 0 })
            {
                if (!string.Equals(confirmedZoneId, zoneId, StringComparison.Ordinal))
                {
                    continue;
                }
            }
            else if (Array.IndexOf(mob.ZoneIds, zoneId) < 0)
            {
                continue;
            }

            ResolveMobMarkers(mob, window.WorldId, window.ZoneInstance, zoneId,
                string.Equals(confirmedZoneId, zoneId, StringComparison.Ordinal), mobCatalog, zoneCatalog, hunts,
                results);
        }
    }

    private static void ResolveMobMarkers(HuntMobDefinition mob, string worldId, int zoneInstance, string zoneId,
        bool zoneConfirmed, HuntMobCatalog mobCatalog, HuntZoneCatalog zoneCatalog, HuntsService hunts,
        List<HuntsMapMarkerPoint> results)
    {
        var activePhase = hunts.PhaseFor(mob.Id, worldId, zoneInstance);
        var poiIds = new HashSet<int>();
        var finalPhase = false;
        if (activePhase is { } phase && mob.Windows.Length > 0)
        {
            var windowIndex = Math.Clamp(phase.WindowNum - 1, 0, mob.Windows.Length - 1);
            var phases = mob.Windows[windowIndex].Phases;
            if (phases.Length > 0)
            {
                var phaseIndex = Math.Clamp(phase.PhaseNum - 1, 0, phases.Length - 1);
                finalPhase = phaseIndex > 0;
                var zonePoiIds = phases[phaseIndex].ZonePoiIds;
                for (var poiIndex = 0; poiIndex < zonePoiIds.Length; poiIndex++)
                {
                    poiIds.Add(zonePoiIds[poiIndex]);
                }
            }
        }
        else
        {
            for (var windowIndex = 0; windowIndex < mob.Windows.Length; windowIndex++)
            {
                var phases = mob.Windows[windowIndex].Phases;
                for (var phaseIndex = 0; phaseIndex < phases.Length; phaseIndex++)
                {
                    if (mob.Rank == "SS" && phases.Length > 1 && phaseIndex == phases.Length - 1)
                    {
                        continue;
                    }

                    var zonePoiIds = phases[phaseIndex].ZonePoiIds;
                    for (var poiIndex = 0; poiIndex < zonePoiIds.Length; poiIndex++)
                    {
                        poiIds.Add(zonePoiIds[poiIndex]);
                    }
                }
            }
        }

        if (mob.Rank != "SS")
        {
            poiIds.RemoveWhere(mobCatalog.IsSsRankPoi);
        }

        var points = new List<HuntPoiEntry>();
        foreach (var poiId in poiIds)
        {
            var found = zoneCatalog.FindPoi(poiId);
            if (found is { } resolved && string.Equals(resolved.ZoneId, zoneId, StringComparison.Ordinal))
            {
                points.Add(resolved.Poi);
            }
        }

        if (points.Count == 0)
        {
            return;
        }

        var finalLocationResolved = finalPhase && zoneConfirmed && points.Count == 1;
        var confirmedPoiId = hunts.ConfirmedPoiIdFor(mob.Id, worldId, zoneInstance);
        if (confirmedPoiId is null && finalLocationResolved)
        {
            confirmedPoiId = points[0].Id;
        }

        var unsightedCount = 0;
        var soleUnsightedPoiId = 0;
        for (var index = 0; index < points.Count; index++)
        {
            if (hunts.IsPoiSighted(mob.Id, worldId, points[index].Id))
            {
                continue;
            }

            unsightedCount++;
            soleUnsightedPoiId = points[index].Id;
        }

        for (var index = 0; index < points.Count; index++)
        {
            var poi = points[index];
            if (confirmedPoiId is { } confirmed && poi.Id != confirmed)
            {
                continue;
            }

            var sighted = hunts.IsPoiSighted(mob.Id, worldId, poi.Id);
            var soleCandidate = unsightedCount == 1 && poi.Id == soleUnsightedPoiId;
            var isConfirmed = confirmedPoiId is { } confirmedId && confirmedId == poi.Id;
            var state = ResolveState(finalLocationResolved, isConfirmed, soleCandidate, sighted);
            var (rawX, rawY) = poi.ParsedLocation();
            results.Add(new HuntsMapMarkerPoint(rawX, rawY, state));
        }
    }

    private static HuntsMapMarkerState ResolveState(bool finalLocation, bool confirmed, bool soleUnsightedCandidate,
        bool sighted)
    {
        if (finalLocation)
        {
            return HuntsMapMarkerState.Final;
        }

        if (confirmed || soleUnsightedCandidate)
        {
            return HuntsMapMarkerState.Confirmed;
        }

        return sighted ? HuntsMapMarkerState.Sighted : HuntsMapMarkerState.Candidate;
    }
}
