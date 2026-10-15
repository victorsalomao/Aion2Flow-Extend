using System.Runtime.CompilerServices;
using Cloris.Aion2Flow.SceneRuntime.Archive;
using Cloris.Aion2Flow.SceneRuntime.Journal;
using Cloris.Aion2Flow.SceneRuntime.Model;
using Cloris.Aion2Flow.SceneRuntime.Observation;
using Cloris.Aion2Flow.SceneRuntime.Playback;
using Cloris.Aion2Flow.SceneRuntime.Projection;

namespace Cloris.Aion2Flow.Tests.SceneRuntime;

/// <summary>T2.4: closing the playback must release its session/projector; LiteMode (trimmed journal + spilled archive) must not break it.</summary>
public sealed class ScenePlaybackReleaseTests
{
    private const int PlayerId = 100;
    private const int BossId = 200;

    [Fact]
    public void DisposeAsync_ReleasesSessionAndProjector()
    {
        var archive = CreatePayload(liteMode: false, entries: 600, out _);

        var (controllerRef, sessionRef) = OpenPlayCloseAndDrop(archive);
        // The cancelled playback loop may need a tick to unwind; give it up to ~2 s.
        for (var i = 0; i < 40 && (controllerRef.IsAlive || sessionRef.IsAlive); i++)
        {
            Thread.Sleep(50);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False(controllerRef.IsAlive, "Controller still reachable after DisposeAsync.");
        Assert.False(sessionRef.IsAlive, "Session/projector still reachable after DisposeAsync.");
        GC.KeepAlive(archive);
    }

    [Fact]
    public void LiteMode_TrimmedJournal_Playback_DoesNotThrow_And_ArchiveKeepsTotals()
    {
        var full = CreatePayload(liteMode: false, entries: 2000, out var fullJournal);
        var lite = CreatePayload(liteMode: true, entries: 2000, out var liteJournal);

        Assert.True(liteJournal.FirstObservationOrdinal > 0, "Lite journal should have been trimmed.");
        Assert.Equal(0, fullJournal.FirstObservationOrdinal);

        // Aggregates in the archived snapshot are independent of the journal.
        Assert.Equal(full.Snapshot.Combatants.Count, lite.Snapshot.Combatants.Count);
        Assert.Equal(TotalDamage(full), TotalDamage(lite));

        // Playback of a trimmed segment must degrade gracefully (no exception).
        var session = new ScenePlaybackSession(new ArchivedScenePlaybackSource(lite));
        var frame = session.Seek(long.MaxValue, TestContext.Current.CancellationToken);
        Assert.NotNull(frame);
    }

    [Fact]
    public void LiteMode_SpilledArchive_Playback_And_Detail_Work()
    {
        var payload = CreatePayload(liteMode: true, entries: 600, out _);
        var path = Path.Combine(Path.GetTempPath(), "Aion2FlowTests", Guid.NewGuid().ToString("N") + ".asf");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            Assert.True(payload.TrySpill(path));
            Assert.True(payload.IsSpilled);

            var session = new ScenePlaybackSession(new ArchivedScenePlaybackSource(payload));
            Assert.NotNull(session.Seek(long.MaxValue, TestContext.Current.CancellationToken));
            Assert.NotNull(payload.CreateDetailDelta(PlayerId));
        }
        finally
        {
            payload.DeleteSpill();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Controller, WeakReference Session) OpenPlayCloseAndDrop(SceneArchivePayload payload)
#pragma warning disable xUnit1031
        => OpenPlayCloseAsync(payload).GetAwaiter().GetResult();
#pragma warning restore xUnit1031

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<(WeakReference Controller, WeakReference Session)> OpenPlayCloseAsync(SceneArchivePayload payload)
    {
        var controller = new ScenePlaybackController(new ArchivedScenePlaybackSource(payload));
        _ = controller.CurrentFrame;
        controller.Play();
        await Task.Delay(50, TestContext.Current.CancellationToken);
        controller.Pause();
        var refs = (new WeakReference(controller), new WeakReference(controller.Session));
        await controller.DisposeAsync();
        return refs;
    }

    private static long TotalDamage(SceneArchivePayload payload)
    {
        var total = 0L;
        foreach (var combatant in payload.Snapshot.Combatants.AsSpan())
            total += combatant.Metrics.DamageAmount;
        return total;
    }

    private static SceneArchivePayload CreatePayload(bool liteMode, int entries, out ObservedEventJournal journal)
    {
        journal = new ObservedEventJournal { DiscardConsumed = liteMode };
        var sceneId = Guid.NewGuid();
        var owner = new SceneReadModelOwner(journal, sceneId, DateTimeOffset.Now);
        for (var i = 1; i <= entries; i++)
        {
            var header = new ObservedEventHeader(
                sceneId,
                new TimelineStamp { OffsetTicks = (1_000L + i * 10L) * TimeSpan.TicksPerMillisecond, ObservationOrdinal = i - 1, FlushId = 1 },
                PlayerId,
                BossId,
                new RawPacketReference(0x0438, 0, i));
            var observation = new CombatWireObservation
            {
                SkillCode = 11000010,
                Damage = 100 + (i % 7),
                HitCount = 1,
                AttemptCount = 1
            };
            journal.Append(in header, in observation);
        }

        journal.CompleteFlush(1);
        owner.Refresh();
        return owner.CreateArchivePayload();
    }
}
