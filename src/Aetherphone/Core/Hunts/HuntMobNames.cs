using Aetherphone.Core.Game;
using Lumina.Excel.Sheets;

namespace Aetherphone.Core.Hunts;

internal static class HuntMobNames
{
    private static readonly Dictionary<uint, string?> nameCache = new();
    private static SheetLanguageGate nameCacheGate;

    public static string? NameFor(uint bnpcNameId)
    {
        var gate = GameSheetLanguage.CurrentGate();
        if (nameCacheGate != gate)
        {
            nameCache.Clear();
            nameCacheGate = gate;
        }

        if (nameCache.TryGetValue(bnpcNameId, out var cached))
        {
            return cached;
        }

        var name = Plugin.DataManager.GetLocalizedSheet<BNpcName>().TryGetRow(bnpcNameId, out var row) &&
            row.Singular.ExtractText() is { Length: > 0 } text
                ? text
                : null;
        nameCache[bnpcNameId] = name;
        return name;
    }
}
