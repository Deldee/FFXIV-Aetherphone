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
    private const float InstanceTextScale = 0.7f;
    private const float InstanceRowGap = 3f;
    private static readonly Vector4 White = new(1f, 1f, 1f, 1f);

    private readonly HuntsMapMarkers markers;
    private readonly ThemeProvider themes;
    private string? instanceLabel;

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
        var labelSize = Typography.Measure(label, TextScale, FontWeight.SemiBold);
        var labelRowWidth = labelSize.X + (IconWidth + IconGap) * scale;

        instanceLabel = markers.ShownInstance is { } instance
            ? string.Format(Loc.T(L.Hunts.NativeMapMarkersInstanceIndicator), instance)
            : null;
        var instanceSize = instanceLabel is { Length: > 0 }
            ? Typography.Measure(instanceLabel, InstanceTextScale, FontWeight.Regular)
            : Vector2.Zero;

        var contentWidth = MathF.Max(labelRowWidth, instanceSize.X);
        var pixelWidth = contentWidth + SidePadding * 2f * scale;
        var pixelHeight = instanceLabel is { Length: > 0 }
            ? ChipHeight * scale + InstanceRowGap * scale + instanceSize.Y
            : ChipHeight * scale;

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
        DrawChip(theme, min, max, scale, instanceLabel);
    }

    private static void DrawChip(PhoneTheme theme, Vector2 min, Vector2 max, float scale, string? instanceLabel)
    {
        var drawList = ImGui.GetForegroundDrawList();
        var rounding = ChipHeight * scale * 0.5f;
        Elevation.Floating(drawList, min, max, rounding, scale, 1f);
        var surface = IconTile.Surface(theme.Accent);
        Squircle.Fill(drawList, min, max, rounding, ImGui.GetColorU32(surface));
        Squircle.Stroke(drawList, min, max, rounding, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.18f)), scale);
        var ink = White;
        var label = Loc.T(L.Hunts.NativeMapMarkersIndicator);
        var labelSize = Typography.Measure(label, TextScale, FontWeight.SemiBold);
        var labelRowWidth = labelSize.X + (IconWidth + IconGap) * scale;
        var labelLeft = (min.X + max.X) * 0.5f - labelRowWidth * 0.5f;
        var labelRowCenterY = min.Y + ChipHeight * scale * 0.5f;
        AppSkin.Icon(drawList, new Vector2(labelLeft + IconWidth * 0.5f * scale, labelRowCenterY),
            FontAwesomeIcon.MapMarkerAlt.ToIconString(), ink, IconScale);
        Typography.Draw(drawList, new Vector2(labelLeft + (IconWidth + IconGap) * scale, labelRowCenterY - labelSize.Y * 0.5f),
            label, ink, TextScale, FontWeight.SemiBold);

        if (instanceLabel is not { Length: > 0 })
        {
            return;
        }

        var instanceSize = Typography.Measure(instanceLabel, InstanceTextScale, FontWeight.Regular);
        var instanceLeft = (min.X + max.X) * 0.5f - instanceSize.X * 0.5f;
        var instanceTop = min.Y + ChipHeight * scale + InstanceRowGap * scale;
        Typography.Draw(drawList, new Vector2(instanceLeft, instanceTop), instanceLabel,
            new Vector4(ink.X, ink.Y, ink.Z, 0.75f), InstanceTextScale, FontWeight.Regular);
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
