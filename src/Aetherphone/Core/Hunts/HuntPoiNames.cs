using Aetherphone.Core.Game;
using Lumina.Excel.Sheets;

namespace Aetherphone.Core.Hunts;

internal static class HuntPoiNames
{
    private static readonly Dictionary<uint, string?> nameCache = new();
    private static SheetLanguageGate nameCacheGate;

    public static string? NameFor(uint placeNameId)
    {
        if (placeNameId == 0)
        {
            return null;
        }

        var gate = GameSheetLanguage.CurrentGate();
        if (nameCacheGate != gate)
        {
            nameCache.Clear();
            nameCacheGate = gate;
        }

        if (nameCache.TryGetValue(placeNameId, out var cached))
        {
            return cached;
        }

        var name = Plugin.DataManager.GetLocalizedSheet<PlaceName>().TryGetRow(placeNameId, out var row) &&
            row.Name.ExtractText() is { Length: > 0 } text
                ? text
                : null;
        nameCache[placeNameId] = name;
        return name;
    }
}
