using Cloris.Aion2Flow.Capture.Diagnostics;
using Cloris.Aion2Flow.Resources.Catalog;
using Cloris.Aion2Flow.SceneRuntime.Stores;
using Cloris.Aion2Flow.Tests.Protocol;

namespace Cloris.Aion2Flow.Tests.Capture;

/// <summary>T2.2: LiteMode (journal trimmed after projection) must yield the same totals as the full journal.</summary>
public sealed class LiteModeJournalParityTests
{
    public static TheoryData<string> Logs()
    {
        var data = new TheoryData<string>();
        foreach (var path in Directory.GetFiles(Path.GetDirectoryName(FixtureHelper.GetPath("logs/" + ReplayScenarioCatalog.CurrentAssassinDirectDamage))!, "*.log").OrderBy(static p => p, StringComparer.Ordinal))
            data.Add(Path.GetFileName(path));
        return data;
    }

    [Theory]
    [MemberData(nameof(Logs))]
    public void Replay_Totals_Are_Identical_In_Full_And_Lite_Mode(string logName)
    {
        CombatResourceRegistry.SetGameResources(ResourceCatalog.Load(ResourceLanguage.TraditionalChinese));
        var path = FixtureHelper.GetPath($"logs/{logName}");

        var full = PacketLogReplayService.Replay(path, liteMode: false);
        var lite = PacketLogReplayService.Replay(path, liteMode: true);

        Assert.Equal(full.ReplayedLines, lite.ReplayedLines);
        Assert.Equal(full.Combatants.Count, lite.Combatants.Count);
        for (var i = 0; i < full.Combatants.Count; i++)
            Assert.Equal(full.Combatants[i], lite.Combatants[i]);

        Assert.Equal(full.Snapshot.MapId, lite.Snapshot.MapId);
        Assert.Equal(full.Snapshot.MapInstanceId, lite.Snapshot.MapInstanceId);
        Assert.Equal(full.Snapshot.Combatants.Count, lite.Snapshot.Combatants.Count);
        foreach (var (id, combatant) in full.Snapshot.Combatants)
        {
            Assert.True(lite.Snapshot.Combatants.TryGetValue(id, out var liteCombatant));
            Assert.Equal(combatant.DamageAmount, liteCombatant.DamageAmount);
        }

        Assert.Equal(full.MapTransitionArchives.Count, lite.MapTransitionArchives.Count);
        Assert.Equal(full.SceneJournal.NextObservationOrdinal, lite.SceneJournal.NextObservationOrdinal);
        if (full.SceneJournal.Count > 4 * 512)
            Assert.True(lite.SceneJournal.Count < full.SceneJournal.Count, "LiteMode must release consumed journal segments.");
        else
            Assert.True(lite.SceneJournal.Count <= full.SceneJournal.Count);
    }
}
