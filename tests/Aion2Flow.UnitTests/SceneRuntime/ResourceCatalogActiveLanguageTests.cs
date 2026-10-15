using System.Runtime.CompilerServices;
using Cloris.Aion2Flow.Resources.Catalog;

namespace Cloris.Aion2Flow.Tests.SceneRuntime;

[CollectionDefinition("ResourceCatalogStatic", DisableParallelization = true)]
public sealed class ResourceCatalogStaticCollection;

/// <summary>T2.6: only the active language pack stays cached; switching drops the previous one.</summary>
[Collection("ResourceCatalogStatic")]
public sealed class ResourceCatalogActiveLanguageTests
{
    [Fact]
    public void Load_KeepsOnlyActiveLanguageCached_And_ReleasesPrevious()
    {
        var previous = LoadAndWeak(ResourceLanguage.Korean);
        var active = ResourceCatalog.Load(ResourceLanguage.TraditionalChinese);

        Assert.Equal(1, ResourceCatalog.CachedLanguageCount);
        for (var i = 0; i < 3 && previous.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False(previous.IsAlive, "ko-KR snapshot still reachable after switching to zh-TW.");
        Assert.Same(active, ResourceCatalog.Load(ResourceLanguage.TraditionalChinese));
    }

    [Fact]
    public void LoadKnownMapIds_DoesNotPinEnglishSnapshot()
    {
        _ = ResourceCatalog.Load(ResourceLanguage.Korean);

        var ids = ResourceCatalog.LoadKnownMapIds();

        Assert.NotEmpty(ids);
        Assert.Equal(1, ResourceCatalog.CachedLanguageCount);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference LoadAndWeak(string language) => new(ResourceCatalog.Load(language));
}
