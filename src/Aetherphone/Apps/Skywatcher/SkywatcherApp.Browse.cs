using Aetherphone.Core;
using Aetherphone.Core.Game;
using Aetherphone.Windows.Components;
using Dalamud.Bindings.ImGui;

namespace Aetherphone.Apps.Skywatcher;

internal sealed partial class SkywatcherApp
{
    private const float PreviewCardHeight = 150f;
    private const int PreviewStripCount = 5;
    private const float ZoneRowHeight = 38f;
    private const float ZoneWeatherColumnWidth = 92f;
    private const float ZoneMiniGlyphRadius = 10f;

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
        var headerCenterY = inner.Min.Y + 24f * scale;
        var textLeft = inner.Min.X;
        if (hasWeather)
        {
            var glyphCenter = new Vector2(inner.Min.X + 24f * scale, headerCenterY);
            DrawMini(forecast[0], glyphCenter, 20f * scale);
            textLeft = inner.Min.X + 56f * scale;
        }

        var nameMaxWidth = inner.Max.X - textLeft;
        var name = Typography.FitText(zone, nameMaxWidth, TextStyles.Headline);
        Typography.Draw(new Vector2(textLeft, inner.Min.Y), name, palette.Ink, TextStyles.Headline);
        if (hasWeather)
        {
            var weatherLine = Typography.FitText(forecast[0].Weather.Name, nameMaxWidth, TextStyles.Subheadline);
            Typography.Draw(new Vector2(textLeft, inner.Min.Y + Typography.LineHeight(TextStyles.Headline)),
                weatherLine, palette.InkSoft, TextStyles.Subheadline);
            DrawPreviewStrip(inner, palette, scale);
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, height));
        ImGui.Dummy(new Vector2(0f, 6f * scale));
        if (UiInteract.HoverClick(card.Min, card.Max))
        {
            OpenDetail(viewedTerritoryId);
        }
    }

    private void DrawPreviewStrip(Rect inner, in SkyPalette palette, float scale)
    {
        var stripTop = inner.Min.Y + 52f * scale;
        var stripHeight = MathF.Max(1f, inner.Max.Y - stripTop);
        var count = Math.Min(forecast.Count, PreviewStripCount);
        var columnWidth = inner.Width / count;
        for (var index = 0; index < count; index++)
        {
            var window = forecast[index];
            var columnCenterX = inner.Min.X + columnWidth * (index + 0.5f);
            var columnMaxWidth = MathF.Max(1f, columnWidth - 4f * scale);
            Marquee.DrawCentered(new MarqueeId("skywatcher.preview.", index), ShortWhen(window), columnCenterX,
                stripTop, columnMaxWidth, TextStyles.Caption2, palette.InkFaint, false);
            var glyphCenter = new Vector2(columnCenterX, stripTop + stripHeight * 0.60f);
            var glyphRadius = MathF.Min(columnWidth * 0.28f, stripHeight * 0.28f);
            DrawMini(window, glyphCenter, glyphRadius);
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

            var nameMaxWidth = MathF.Max(1f, inner.Width - 24f * scale - ZoneWeatherColumnWidth * scale);
            var name = Typography.FitText(entry.ZoneName, nameMaxWidth, TextStyles.Body);
            var nameSize = Typography.Measure(name);
            Typography.Draw(new Vector2(inner.Min.X + 12f * scale, rowCenterY - nameSize.Y * 0.5f), name, palette.Ink);

            var entryWeather = weather.Entry(weather.NaturalNow(entry.TerritoryId));
            var nowWindow = new WeatherWindow(entryWeather, 0, true, 0);
            var glyphCenter = new Vector2(inner.Max.X - ZoneWeatherColumnWidth * scale + ZoneMiniGlyphRadius * scale,
                rowCenterY);
            DrawMini(nowWindow, glyphCenter, ZoneMiniGlyphRadius * scale);
            var weatherNameLeft = glyphCenter.X + ZoneMiniGlyphRadius * scale + 8f * scale;
            var weatherNameMaxWidth = MathF.Max(1f, inner.Max.X - 10f * scale - weatherNameLeft);
            var weatherName = Typography.FitText(entryWeather.Name, weatherNameMaxWidth, 1f, FontWeight.Regular);
            var weatherNameSize = Typography.Measure(weatherName);
            Typography.Draw(
                new Vector2(inner.Max.X - 10f * scale - weatherNameSize.X, rowCenterY - weatherNameSize.Y * 0.5f),
                weatherName, palette.InkSoft);
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
