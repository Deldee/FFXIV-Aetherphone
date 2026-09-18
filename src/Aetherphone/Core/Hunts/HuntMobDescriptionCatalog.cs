using Aetherphone.Core.Game;
using Lumina.Excel;

namespace Aetherphone.Core.Hunts;

internal sealed class HuntMobDescriptionCatalog
{
    private const int DescriptionColumn = 1;

    private readonly HuntJsonCatalogLoader<Dictionary<string, HuntMobDescriptionReference>> loader;
    private Dictionary<string, HuntMobDescriptionReference> byMobId = new();
    private readonly Dictionary<string, string?> resolvedCache = new();
    private SheetLanguageGate resolvedCacheGate;

    public HuntMobDescriptionCatalog(FileInfo source)
    {
        loader = new HuntJsonCatalogLoader<Dictionary<string, HuntMobDescriptionReference>>(source,
            HuntMobDescriptionCatalogJsonContext.Default.DictionaryStringHuntMobDescriptionReference,
            "mob description catalog", parsed => byMobId = parsed);
        loader.Preload();
    }

    public IReadOnlyDictionary<string, HuntMobDescriptionReference> ById
    {
        get
        {
            loader.EnsureLoaded();
            return byMobId;
        }
    }

    public string? DescriptionFor(string mobId)
    {
        loader.EnsureLoaded();
        if (!byMobId.TryGetValue(mobId, out var reference))
        {
            return null;
        }

        var gate = GameSheetLanguage.CurrentGate();
        if (resolvedCacheGate != gate)
        {
            resolvedCache.Clear();
            resolvedCacheGate = gate;
        }

        if (resolvedCache.TryGetValue(mobId, out var cached))
        {
            return cached;
        }

        var resolved = ResolveText(reference);
        resolvedCache[mobId] = resolved;
        return resolved;
    }

    private static string? ResolveText(HuntMobDescriptionReference reference)
    {
        var sheet = Plugin.DataManager.GetLocalizedSheet<RawRow>(sheetName: reference.Sheet);
        return sheet.TryGetRow(reference.Row, out var row) ? row.ReadStringColumn(DescriptionColumn).ExtractText() : null;
    }
}
