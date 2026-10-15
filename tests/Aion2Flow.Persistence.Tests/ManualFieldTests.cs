using Cloris.Aion2Flow.Domain.Progress;
using Xunit;

namespace Cloris.Aion2Flow.Persistence.Tests;

public class ManualFieldTests
{
    private sealed class FakeTime(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Notifier : IResinNotifier
    {
        public List<ResinFullNotification> Sent { get; } = [];
        public ValueTask NotifyFullAsync(ResinFullNotification n, CancellationToken ct = default) { Sent.Add(n); return ValueTask.CompletedTask; }
    }

    [Fact]
    public async Task Cascade_delete_removes_fields()
    {
        using var db = SqliteDatabase.InMemory();
        var alts = new SqliteAltRepository(db);
        var a = new Alt(AltId.New(), "A", "S", "C", 1);
        await alts.SaveAsync(a);
        var repo = new SqliteManualFieldRepository(db);
        await repo.SaveAsync(ManualField.Create(a.Id, "kinah", 10, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch));
        Assert.Single(await repo.ListByAltAsync(a.Id));
        await alts.DeleteAsync(a.Id);
        Assert.Empty(await repo.ListByAltAsync(a.Id));
    }

    [Fact]
    public async Task Delta_today_resets_after_daily_reset()
    {
        using var db = SqliteDatabase.InMemory();
        var a = new Alt(AltId.New(), "A", "S", "C", 1);
        await new SqliteAltRepository(db).SaveAsync(a);
        var time = new FakeTime(new DateTimeOffset(2026, 10, 14, 0, 0, 0, TimeSpan.Zero)); // 09:00 KR
        var svc = new ManualFieldService(new SqliteManualFieldRepository(db), new ResetClock(ServerRegion.KR, time), time);
        await svc.SetAsync(a.Id, "kinah", 100);
        time.Now += TimeSpan.FromHours(1);
        await svc.SetAsync(a.Id, "kinah", 150);
        Assert.Equal(50, await svc.TodayDeltaAsync(a.Id, "kinah"));
        time.Now += TimeSpan.FromDays(1);
        Assert.Equal(0, await svc.TodayDeltaAsync(a.Id, "kinah"));
        await svc.SetAsync(a.Id, "kinah", 170);
        Assert.Equal(20, await svc.TodayDeltaAsync(a.Id, "kinah"));
    }

    [Fact]
    public void Last_resets_are_prior_boundaries()
    {
        var clock = new ResetClock(ServerRegion.KR, new FakeTime(new DateTimeOffset(2026, 10, 14, 0, 0, 0, TimeSpan.Zero)));
        Assert.Equal(clock.NextDaily().AddDays(-1), clock.LastDaily());
        Assert.Equal(clock.NextWeekly().AddDays(-7), clock.LastWeekly());
    }

    [Fact]
    public void Resin_projection_and_full_time()
    {
        var t = DateTimeOffset.UnixEpoch;
        var rule = new RechargeRule("resin", TimeSpan.FromMinutes(10));
        var r = new Resource(AltId.New(), "resin", 10, 20, t);
        Assert.Equal(15, ResinCalculator.Project(r, rule, t.AddMinutes(55)));
        Assert.Equal(20, ResinCalculator.Project(r, rule, t.AddDays(1)));
        Assert.Equal(t.AddMinutes(100), ResinCalculator.FullAt(r, rule));
    }

    [Fact]
    public async Task Monitor_notifies_once_when_full()
    {
        using var db = SqliteDatabase.InMemory();
        var a = new Alt(AltId.New(), "A", "S", "C", 1);
        await new SqliteAltRepository(db).SaveAsync(a);
        var res = new SqliteResourceRepository(db);
        var t = DateTimeOffset.UnixEpoch;
        await res.SaveAsync(new Resource(a.Id, "resin", 10, 20, t));
        var time = new FakeTime(t.AddMinutes(50));
        var n = new Notifier();
        var mon = new ResinMonitor(res, n, new RechargeRule("resin", TimeSpan.FromMinutes(10)), time);
        Assert.Equal(0, await mon.CheckAsync([a.Id]));
        time.Now = t.AddMinutes(101);
        Assert.Equal(1, await mon.CheckAsync([a.Id]));
        Assert.Equal(0, await mon.CheckAsync([a.Id]));
        Assert.Single(n.Sent);
    }
}
