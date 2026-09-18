using Aetherphone.Core.Game;
using Lumina.Excel.Sheets;

namespace Aetherphone.Core.Hunts;

internal static class HuntRewardItems
{
    private static readonly Dictionary<uint, string?> nameCache = new();
    private static SheetLanguageGate nameCacheGate;

    public static uint IconIdFor(uint itemRowId) =>
        Plugin.DataManager.GetExcelSheet<Item>().TryGetRow(itemRowId, out var row) ? row.Icon : 0u;

    public static string? NameFor(uint itemRowId)
    {
        var gate = GameSheetLanguage.CurrentGate();
        if (nameCacheGate != gate)
        {
            nameCache.Clear();
            nameCacheGate = gate;
        }

        if (nameCache.TryGetValue(itemRowId, out var cached))
        {
            return cached;
        }

        var name = Plugin.DataManager.GetLocalizedSheet<Item>().TryGetRow(itemRowId, out var row) &&
            row.Name.ExtractText() is { Length: > 0 } text
                ? text
                : null;
        nameCache[itemRowId] = name;
        return name;
    }
}
