using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace Cloris.Aion2Flow.Resources.Catalog;

public static class ResourceCatalog
{
    private static readonly Lazy<ResourceSharedCatalog> Shared = new(ResourcePackReader.LoadShared);
    private static readonly ConcurrentDictionary<string, ResourceCatalogSnapshot> Snapshots = new(StringComparer.Ordinal);

    public static ResourceSharedCatalog LoadShared() => Shared.Value;

    /// <summary>Number of locale snapshots currently cached (T2.6: at most the active language).</summary>
    public static int CachedLanguageCount => Snapshots.Count;

    /// <summary>
    /// Loads the snapshot for <paramref name="language"/> and drops the cached snapshots of every other
    /// language, so only the active locale pack stays resident (shared data is language-independent).
    /// </summary>
    public static ResourceCatalogSnapshot Load(string language = ResourceLanguage.English)
    {
        if (!ResourceLanguage.IsSupported(language))
        {
            throw new ArgumentOutOfRangeException(nameof(language), language, "Unsupported resource language.");
        }

        var snapshot = Snapshots.GetOrAdd(language, static lang => new ResourceCatalogSnapshot(LoadShared(), ResourcePackReader.LoadLocale(lang)));
        foreach (var key in Snapshots.Keys)
        {
            if (!string.Equals(key, language, StringComparison.Ordinal))
                Snapshots.TryRemove(key, out _);
        }

        return snapshot;
    }

    /// <summary>
    /// Known map ids without pinning the English locale pack: reuses the cached English snapshot when present,
    /// otherwise decodes the pack transiently and lets it be collected.
    /// </summary>
    public static FrozenSet<uint> LoadKnownMapIds()
        => (Snapshots.TryGetValue(ResourceLanguage.English, out var english)
            ? english.Maps
            : ResourcePackReader.LoadLocale(ResourceLanguage.English).Maps).Keys.ToFrozenSet();
}
