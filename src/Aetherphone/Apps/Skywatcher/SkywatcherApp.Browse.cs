using Aetherphone.Core;
using Aetherphone.Core.Game;
using Aetherphone.Windows.Components;
using Dalamud.Bindings.ImGui;

namespace Aetherphone.Apps.Skywatcher;

internal sealed partial class SkywatcherApp
{
    private const float PreviewCardHeight = 92f;
    private const float ZoneRowHeight = 38f;

    private void DrawBrowse(in SkyPalette palette, float scale)
    {
        DrawCurrentAreaPreview(palette, scale);

        var regions = weather.ZonesByRegion();
        for (var regionIndex = 0; regionIndex < regions.Count; regionIndex++)
        {
            var region = regions[regionIndex];
            if (CountOtherZones(region.Zones) == 0)
            {
                continue;
            }

            SectionLabel(region.Region, palette, scale);
            DrawZoneList(palette, scale, region.Zones);
        }

        ImGui.Dummy(new Vector2(0f, 8f * scale));
    }

    private void DrawCurrentAreaPreview(in SkyPalette palette, float scale)
    {
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var height = PreviewCardHeight * scale;
        var card = new Rect(origin, origin + new Vector2(width, height));
        DrawGlass(card, palette, scale);
        var inner = card.Inset(14f * scale);
        var hasWeather = forecast.Count > 0;
        var textLeft = inner.Min.X;
        if (hasWeather)
        {
            var glyphCenter = new Vector2(inner.Min.X + 26f * scale, inner.Center.Y);
            DrawMini(forecast[0], glyphCenter, 22f * scale);
            textLeft = inner.Min.X + 60f * scale;
        }

        var nameMaxWidth = inner.Max.X - textLeft;
        var name = Typography.FitText(zone, nameMaxWidth, TextStyles.Headline);
        Typography.Draw(new Vector2(textLeft, inner.Min.Y + 2f * scale), name, palette.Ink, TextStyles.Headline);
        if (hasWeather)
        {
            var weatherLine = Typography.FitText(forecast[0].Weather.Name, nameMaxWidth, TextStyles.Subheadline);
            Typography.Draw(
                new Vector2(textLeft, inner.Min.Y + 2f * scale + Typography.LineHeight(TextStyles.Headline)),
                weatherLine, palette.InkSoft, TextStyles.Subheadline);
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, height));
        ImGui.Dummy(new Vector2(0f, 6f * scale));
        if (UiInteract.HoverClick(card.Min, card.Max))
        {
            OpenDetail(viewedTerritoryId);
        }
    }

    private void DrawZoneList(in SkyPalette palette, float scale, IReadOnlyList<WeatherZoneEntry> zones)
    {
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var rowHeight = ZoneRowHeight * scale;
        var visibleCount = CountOtherZones(zones);
        var card = new Rect(origin, origin + new Vector2(width, visibleCount * rowHeight + 10f * scale));
        DrawGlass(card, palette, scale);
        var inner = card.Inset(5f * scale);
        var drawList = ImGui.GetWindowDrawList();
        var rowIndex = 0;
        for (var index = 0; index < zones.Count; index++)
        {
            var entry = zones[index];
            if (entry.TerritoryId == viewedTerritoryId)
            {
                continue;
            }

            var rowTop = inner.Min.Y + rowIndex * rowHeight;
            var rowCenterY = rowTop + rowHeight * 0.5f;
            if (rowIndex > 0)
            {
                drawList.AddLine(new Vector2(inner.Min.X + 12f * scale, rowTop),
                    new Vector2(inner.Max.X - 10f * scale, rowTop), ImGui.GetColorU32(palette.Ink with { W = 0.10f }),
                    1f);
            }

            var nameMaxWidth = inner.Width - 24f * scale;
            var name = Typography.FitText(entry.ZoneName, nameMaxWidth, TextStyles.Body);
            var nameSize = Typography.Measure(name);
            Typography.Draw(new Vector2(inner.Min.X + 12f * scale, rowCenterY - nameSize.Y * 0.5f), name, palette.Ink);
            if (UiInteract.HoverClick(new Vector2(inner.Min.X, rowTop), new Vector2(inner.Max.X, rowTop + rowHeight)))
            {
                OpenDetail(entry.TerritoryId);
            }

            rowIndex++;
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, card.Height));
    }

    private int CountOtherZones(IReadOnlyList<WeatherZoneEntry> zones)
    {
        var count = 0;
        for (var index = 0; index < zones.Count; index++)
        {
            if (zones[index].TerritoryId != viewedTerritoryId)
            {
                count++;
            }
        }

        return count;
    }
}
