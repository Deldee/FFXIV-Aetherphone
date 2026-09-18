using Aetherphone.Core.Maps;

namespace Aetherphone.Core.Hunts;

internal sealed class HuntZoneCatalog
{
    private readonly HuntJsonCatalogLoader<Dictionary<string, HuntZoneDefinition>> loader;
    private Dictionary<string, HuntZoneDefinition> byId = new();
    private Dictionary<int, (string ZoneId, HuntPoiEntry Poi)> poisById = new();
    private Dictionary<uint, string>? zoneIdByTerritory;

    public HuntZoneCatalog(FileInfo source)
    {
        loader = new HuntJsonCatalogLoader<Dictionary<string, HuntZoneDefinition>>(source,
            HuntZoneJsonContext.Default.DictionaryStringHuntZoneDefinition, "zone catalog", OnLoaded);
        loader.Preload();
    }

    public HuntZoneDefinition? FindZone(string zoneId)
    {
        loader.EnsureLoaded();
        return byId.GetValueOrDefault(zoneId);
    }

    public (string ZoneId, HuntPoiEntry Poi)? FindPoi(int poiId)
    {
        loader.EnsureLoaded();
        return poisById.TryGetValue(poiId, out var entry) ? entry : null;
    }

    public (float X, float Y)? ResolveCoordinate(int poiId)
    {
        var found = FindPoi(poiId);
        if (found is not { } resolved)
        {
            return null;
        }

        if (!byId.TryGetValue(resolved.ZoneId, out var zone))
        {
            return null;
        }

        return ToGameCoordinate(resolved.Poi, zone.Map);
    }

    public uint ResolveTerritoryId(string zoneId) => FindZone(zoneId)?.TerritoryId ?? 0u;

    public string? ZoneIdForTerritory(uint territoryId)
    {
        loader.EnsureLoaded();
        var lookup = zoneIdByTerritory ??= BuildZoneIdByTerritoryLookup();
        return lookup.TryGetValue(territoryId, out var zoneId) ? zoneId : null;
    }

    private Dictionary<uint, string> BuildZoneIdByTerritoryLookup()
    {
        var lookup = new Dictionary<uint, string>();
        foreach (var zone in byId.Values)
        {
            var territoryId = ResolveTerritoryId(zone.Id);
            if (territoryId != 0)
            {
                lookup[territoryId] = zone.Id;
            }
        }

        return lookup;
    }

    private void OnLoaded(Dictionary<string, HuntZoneDefinition> parsed)
    {
        byId = parsed;
        var index = new Dictionary<int, (string, HuntPoiEntry)>();
        foreach (var zone in parsed.Values)
        {
            foreach (var poi in zone.Pois)
            {
                index[poi.Id] = (zone.Id, poi);
            }
        }

        poisById = index;
    }

    private static (float X, float Y) ToGameCoordinate(HuntPoiEntry poi, HuntZoneMap map)
    {
        var (rawX, rawY) = poi.ParsedLocation();
        return MapPixelMath.ToGameCoordinate(rawX, rawY, map.SizeFactor);
    }
}
