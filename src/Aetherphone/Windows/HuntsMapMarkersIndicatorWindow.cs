using Aetherphone.Core;
using Aetherphone.Core.Localization;
using Aetherphone.Core.Maps;
using Aetherphone.Core.Theme;
using Aetherphone.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Aetherphone.Windows;

internal sealed unsafe class HuntsMapMarkersIndicatorWindow : Window
{
    private const ImGuiWindowFlags ChipFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar |
                                               ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse |
                                               ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoBackground |
                                               ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoFocusOnAppearing |
                                               ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoSavedSettings |
                                               ImGuiWindowFlags.NoInputs;

    private const string AreaMapAddonName = "AreaMap";
    private const float CornerInset = 10f;
    private const float ChipHeight = 28f;
    private const float SidePadding = 12f;
    private const float IconGap = 7f;
    private const float IconScale = 0.66f;
    private const float IconWidth = 11f;
    private const float TextScale = 0.8f;
    private static readonly Vector4 White = new(1f, 1f, 1f, 1f);

    private readonly HuntsMapMarkers markers;
    private readonly ThemeProvider themes;

    public HuntsMapMarkersIndicatorWindow(HuntsMapMarkers markers, ThemeProvider themes)
        : base($"{AepConstants.Name}##HuntsMapMarkersIndicator", ChipFlags)
    {
        this.markers = markers;
        this.themes = themes;
        IsOpen = true;
        RespectCloseHotkey = false;
    }

    public override bool DrawConditions() => markers.HasActiveMarkers && TryGetAreaMapBounds(out _, out _);

    public override void PreDraw()
    {
        TryGetAreaMapBounds(out var mapPosition, out var mapSize);
        var scale = UiScale.Global;
        var label = Loc.T(L.Hunts.NativeMapMarkersIndicator);
        var textWidth = Typography.Measure(label, TextScale, FontWeight.SemiBold).X;
        var pixelWidth = textWidth + (SidePadding * 2f + IconWidth + IconGap) * scale;
        var pixelHeight = ChipHeight * scale;
        Size = new Vector2(pixelWidth / scale, pixelHeight / scale);
        SizeCondition = ImGuiCond.Always;
        Position = new Vector2(mapPosition.X + mapSize.X - pixelWidth - CornerInset * scale,
            mapPosition.Y + CornerInset * scale);
        PositionCondition = ImGuiCond.Always;
    }

    public override void Draw()
    {
        var scale = UiScale.Global;
        var theme = themes.Chrome;
        var min = ImGui.GetWindowPos();
        var max = min + ImGui.GetWindowSize();
        DrawChip(theme, min, max, scale);
    }

    private static void DrawChip(PhoneTheme theme, Vector2 min, Vector2 max, float scale)
    {
        var drawList = ImGui.GetForegroundDrawList();
        var rounding = (max.Y - min.Y) * 0.5f;
        Elevation.Floating(drawList, min, max, rounding, scale, 1f);
        var surface = IconTile.Surface(theme.Accent);
        Squircle.Fill(drawList, min, max, rounding, ImGui.GetColorU32(surface));
        Squircle.Stroke(drawList, min, max, rounding, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.18f)), scale);
        var ink = White;
        var label = Loc.T(L.Hunts.NativeMapMarkersIndicator);
        var textSize = Typography.Measure(label, TextScale, FontWeight.SemiBold);
        var contentWidth = textSize.X + (IconWidth + IconGap) * scale;
        var left = (min.X + max.X) * 0.5f - contentWidth * 0.5f;
        var centerY = (min.Y + max.Y) * 0.5f;
        AppSkin.Icon(drawList, new Vector2(left + IconWidth * 0.5f * scale, centerY),
            FontAwesomeIcon.MapMarkerAlt.ToIconString(), ink, IconScale);
        Typography.Draw(drawList, new Vector2(left + (IconWidth + IconGap) * scale, centerY - textSize.Y * 0.5f),
            label, ink, TextScale, FontWeight.SemiBold);
    }

    private static bool TryGetAreaMapBounds(out Vector2 position, out Vector2 size)
    {
        position = default;
        size = default;
        var addon = (AtkUnitBase*)Plugin.GameGui.GetAddonByName(AreaMapAddonName).Address;
        if (addon == null || !addon->IsVisible)
        {
            return false;
        }

        position = new Vector2(addon->X, addon->Y);
        size = new Vector2(addon->GetScaledWidth(true), addon->GetScaledHeight(true));
        return true;
    }
}
