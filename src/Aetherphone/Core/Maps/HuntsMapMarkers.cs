using Aetherphone.Core.Hunts;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
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
    private const string AreaMapAddonName = "AreaMap";
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(2);

    private readonly Configuration configuration;
    private readonly HuntsService hunts;
    private readonly HuntMobCatalog mobCatalog;
    private readonly HuntZoneCatalog zoneCatalog;
    private readonly List<HuntsMapMarkerPoint> points = new();
    private readonly HashSet<HuntsMapMarkerPoint> lastPlacedPoints = new();
    private bool hasPlacedMarkers;
    private uint cachedTerritoryId;
    private string cachedWorldId = string.Empty;
    private DateTime lastRefreshUtc = DateTime.MinValue;
    private bool forceRedraw = true;

    public HuntsMapMarkers(Configuration configuration, HuntsService hunts, HuntMobCatalog mobCatalog,
        HuntZoneCatalog zoneCatalog)
    {
        this.configuration = configuration;
        this.hunts = hunts;
        this.mobCatalog = mobCatalog;
        this.zoneCatalog = zoneCatalog;
        Plugin.Framework.Update += OnFrameworkUpdate;
        Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, AreaMapAddonName, OnAreaMapOpenedOrChanged);
        Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, AreaMapAddonName, OnAreaMapOpenedOrChanged);
    }

    public void Dispose()
    {
        Plugin.Framework.Update -= OnFrameworkUpdate;
        Plugin.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, AreaMapAddonName, OnAreaMapOpenedOrChanged);
        Plugin.AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, AreaMapAddonName, OnAreaMapOpenedOrChanged);
    }

    private void OnAreaMapOpenedOrChanged(AddonEvent type, AddonArgs args)
    {
        lastRefreshUtc = DateTime.MinValue;
        forceRedraw = true;
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

        var territoryId = agentMap->SelectedTerritoryId;
        var worldId = HuntDataCenterWorlds.SlugFor(LocationShare.CurrentWorldId());
        var contextChanged = territoryId != cachedTerritoryId ||
            !string.Equals(worldId, cachedWorldId, StringComparison.OrdinalIgnoreCase);
        if (contextChanged)
        {
            forceRedraw = true;
        }

        if (!contextChanged && !forceRedraw && DateTime.UtcNow - lastRefreshUtc < RefreshInterval)
        {
            return;
        }

        cachedTerritoryId = territoryId;
        cachedWorldId = worldId;
        lastRefreshUtc = DateTime.UtcNow;
        var mustRedraw = forceRedraw;
        forceRedraw = false;

        if (!TryResolveTarget(territoryId, worldId, out var map))
        {
            ClearNativeMarkersIfNeeded(agentMap);
            return;
        }

        if (!mustRedraw && hasPlacedMarkers && points.Count == lastPlacedPoints.Count &&
            lastPlacedPoints.SetEquals(points))
        {
            return;
        }

        agentMap->ResetMapMarkers();
        agentMap->ResetMiniMapMarkers();
        hasPlacedMarkers = true;
        lastPlacedPoints.Clear();

        for (var index = 0; index < points.Count; index++)
        {
            var point = points[index];
            lastPlacedPoints.Add(point);
            var worldX = MapPixelMath.ToWorldCoordinate(point.RawX, map.SizeFactor, map.OffsetX);
            var worldZ = MapPixelMath.ToWorldCoordinate(point.RawY, map.SizeFactor, map.OffsetY);
            var worldPosition = new Vector3(worldX, 0f, worldZ);
            var iconId = IconFor(point.State);
            agentMap->AddMapMarker(worldPosition, iconId, MarkerScale);
            agentMap->AddMiniMapMarker(worldPosition, iconId, MarkerScale);
        }
    }

    private bool TryResolveTarget(uint territoryId, string worldId, out Map map)
    {
        map = default;

        var zoneId = zoneCatalog.ZoneIdForTerritory(territoryId);
        if (zoneId is not { Length: > 0 } || worldId.Length == 0)
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
        lastPlacedPoints.Clear();
    }

    private static uint IconFor(HuntsMapMarkerState state) => state switch
    {
        HuntsMapMarkerState.Sighted => SightedIconId,
        HuntsMapMarkerState.Confirmed => ConfirmedIconId,
        HuntsMapMarkerState.Final => FinalIconId,
        _ => CandidateIconId,
    };
}
