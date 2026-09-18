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

    public string? DescriptionFor(string mobId, HuntMobDefinition? def)
    {
        var fatePhaseNameIds = def is not null ? FatePhaseNameIds(def) : null;
        loader.EnsureLoaded();
        var hasCatalogReference = byMobId.TryGetValue(mobId, out var reference);
        if (fatePhaseNameIds is null && !hasCatalogReference)
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

        string? resolved;
        if (fatePhaseNameIds is { Count: > 0 })
        {
            resolved = ResolveFromFatePhases(fatePhaseNameIds);
        }
        else if (hasCatalogReference)
        {
            resolved = ResolveText(reference!);
        }
        else
        {
            resolved = null;
        }

        resolvedCache[mobId] = resolved;
        return resolved;
    }

    private static List<uint>? FatePhaseNameIds(HuntMobDefinition def)
    {
        List<uint>? nameIds = null;
        foreach (var window in def.Windows)
        {
            foreach (var phase in window.Phases)
            {
                if (phase.NameId == 0)
                {
                    continue;
                }

                (nameIds ??= new List<uint>()).Add(phase.NameId);
            }
        }

        return nameIds;
    }

    private static string? ResolveFromFatePhases(List<uint> fateRowIds)
    {
        List<string>? parts = null;
        foreach (var rowId in fateRowIds)
        {
            if (ResolveText(new HuntMobDescriptionReference { Sheet = "Fate", Row = rowId }) is { Length: > 0 } text)
            {
                (parts ??= new List<string>()).Add(text);
            }
        }

        return parts is null ? null : string.Join(" ", parts);
    }

    private static string? ResolveText(HuntMobDescriptionReference reference)
    {
        var sheet = Plugin.DataManager.GetLocalizedSheet<RawRow>(sheetName: reference.Sheet);
        List<string>? parts = null;
        for (var index = 0; index < reference.Count; index++)
        {
            if (!sheet.TryGetRow(reference.Row + (uint)index, out var row))
            {
                break;
            }

            var text = StripDecorativeQuotes(row.ReadStringColumn(DescriptionColumn).ExtractText());
            if (text.Length == 0)
            {
                break;
            }

            (parts ??= new List<string>()).Add(text);
        }

        return parts is null ? null : string.Join(" ", parts);
    }

    private static string StripDecorativeQuotes(string text)
    {
        if (text.Length > 0 && text[0] is '"' or '“' or '”')
        {
            text = text[1..];
        }

        if (text.Length > 0 && text[^1] is '"' or '“' or '”')
        {
            text = text[..^1];
        }

        return text;
    }
}
