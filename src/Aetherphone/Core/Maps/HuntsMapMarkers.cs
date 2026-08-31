using Aetherphone.Core.Hunts;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace Aetherphone.Core.Maps;

internal sealed class HuntsMapMarkers : IDisposable
{
    private const uint CandidateIconId = 60557u;
    private const uint SightedIconId = 60444u;
    private const uint ConfirmedIconId = 60403u;
    private const uint FinalIconId = 60422u;
    private const int MarkerScale = 600;

    private readonly Configuration configuration;
    private readonly HuntsService hunts;
    private readonly HuntMobCatalog mobCatalog;
    private readonly HuntZoneCatalog zoneCatalog;
    private readonly List<HuntsMapMarkerPoint> points = new();
    private bool hasPlacedMarkers;

    public HuntsMapMarkers(Configuration configuration, HuntsService hunts, HuntMobCatalog mobCatalog,
        HuntZoneCatalog zoneCatalog)
    {
        this.configuration = configuration;
        this.hunts = hunts;
        this.mobCatalog = mobCatalog;
        this.zoneCatalog = zoneCatalog;
        Plugin.Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        Plugin.Framework.Update -= OnFrameworkUpdate;
    }

    private unsafe void OnFrameworkUpdate(IFramework framework)
    {
        if (!configuration.HuntsNativeMapMarkers)
        {
            return;
        }

        var agentMap = AgentMap.Instance();
        if (agentMap == null)
        {
            return;
        }

        if (!TryResolveTarget(agentMap, out var map))
        {
            ClearNativeMarkersIfNeeded(agentMap);
            return;
        }

        agentMap->ResetMapMarkers();
        agentMap->ResetMiniMapMarkers();
        hasPlacedMarkers = true;

        for (var index = 0; index < points.Count; index++)
        {
            var point = points[index];
            var worldX = MapPixelMath.ToWorldCoordinate(point.RawX, map.SizeFactor, map.OffsetX);
            var worldZ = MapPixelMath.ToWorldCoordinate(point.RawY, map.SizeFactor, map.OffsetY);
            var worldPosition = new Vector3(worldX, 0f, worldZ);
            var iconId = IconFor(point.State);
            agentMap->AddMapMarker(worldPosition, iconId, MarkerScale);
            agentMap->AddMiniMapMarker(worldPosition, iconId, MarkerScale);
        }
    }

    private unsafe bool TryResolveTarget(AgentMap* agentMap, out Map map)
    {
        map = default;

        var territoryId = agentMap->SelectedTerritoryId;
        var zoneId = zoneCatalog.ZoneIdForTerritory(territoryId);
        if (zoneId is not { Length: > 0 })
        {
            return false;
        }

        var worldId = HuntDataCenterWorlds.SlugFor(LocationShare.CurrentWorldId());
        if (worldId.Length == 0)
        {
            return false;
        }

        HuntCandidateResolver.ResolveZoneMarkers(zoneId, worldId, mobCatalog, zoneCatalog, hunts, points);
        if (points.Count == 0)
        {
            return false;
        }

        if (!Plugin.DataManager.GetExcelSheet<TerritoryType>().TryGetRow(territoryId, out var territory) ||
            !Plugin.DataManager.GetExcelSheet<Map>().TryGetRow(territory.Map.RowId, out var resolvedMap))
        {
            return false;
        }

        map = resolvedMap;
        return true;
    }

    private unsafe void ClearNativeMarkersIfNeeded(AgentMap* agentMap)
    {
        if (!hasPlacedMarkers)
        {
            return;
        }

        agentMap->ResetMapMarkers();
        agentMap->ResetMiniMapMarkers();
        hasPlacedMarkers = false;
    }

    private static uint IconFor(HuntsMapMarkerState state) => state switch
    {
        HuntsMapMarkerState.Sighted => SightedIconId,
        HuntsMapMarkerState.Confirmed => ConfirmedIconId,
        HuntsMapMarkerState.Final => FinalIconId,
        _ => CandidateIconId,
    };
}
