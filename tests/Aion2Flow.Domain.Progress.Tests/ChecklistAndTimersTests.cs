using Xunit;

namespace Cloris.Aion2Flow.Domain.Progress.Tests;

public class ChecklistAndTimersTests
{
    private sealed class FakeTime(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class MemRepo : ITaskProgressRepository
    {
        private readonly Dictionary<(AltId, TaskId), TaskProgress> _data = [];

        public Task<IReadOnlyList<TaskProgress>> ListByAltAsync(AltId a, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<TaskProgress>>(_data.Values.Where(p => p.AltId == a).ToList());

        public Task SaveAsync(TaskProgress p, CancellationToken ct = default)
        {
            _data[(p.AltId, p.TaskId)] = p;
            return Task.CompletedTask;
        }
    }

    private static DateTimeOffset Utc(int y, int m, int d, int h = 0, int mi = 0) => new(y, m, d, h, mi, 0, TimeSpan.Zero);

    private const string TasksJson = """
        { "tasks": [
          {"id":"d","name":"Daily","type":"daily","maxCount":1},
          {"id":"w","name":"Weekly","type":"weekly","maxCount":2},
          {"id":"bad","name":"","type":"daily","maxCount":1} ] }
        """;

    private static (ChecklistService svc, FakeTime time) Make(DateTimeOffset now)
    {
        var time = new FakeTime(now);
        var clock = new ResetClock(ServerRegion.KR, time);
        return (new ChecklistService(TaskCatalog.Parse(TasksJson), new MemRepo(), clock, time), time);
    }

    [Fact]
    public void Loader_ParsesAndDropsInvalid()
    {
        var c = TaskCatalog.Parse(TasksJson);
        Assert.Equal(2, c.Tasks.Count);
        Assert.Equal(TaskCadence.Weekly, c.Tasks[1].Cadence);
    }

    [Fact]
    public void Loader_MissingFile_IsEmpty()
    {
        Assert.Empty(TaskCatalog.Load("nao-existe.json").Tasks);
        Assert.Empty(EventCatalog.Load("nao-existe.json").Events);
    }

    [Fact]
    public void Loader_ShippedFiles_AreValid()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null && !Directory.Exists(Path.Combine(dir, "data"))) dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        Assert.NotEmpty(TaskCatalog.Load(Path.Combine(dir, "data", "tasks.json")).Tasks);
        Assert.NotEmpty(EventCatalog.Load(Path.Combine(dir, "data", "events.json")).Events);
    }

    [Fact]
    public async Task Toggle_MarksAndUnmarks()
    {
        var (svc, _) = Make(Utc(2026, 3, 10, 0));
        var alt = AltId.New();
        Assert.True((await svc.ToggleAsync(alt, "d", TestContext.Current.CancellationToken))!.IsDone);
        Assert.Equal(ChecklistState.Done, (await svc.GetAsync(alt, TestContext.Current.CancellationToken))[0].State);
        Assert.False((await svc.ToggleAsync(alt, "d", TestContext.Current.CancellationToken))!.IsDone);
        Assert.Null(await svc.ToggleAsync(alt, "x", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Daily_ResetsAt0500Local()
    {
        var (svc, time) = Make(Utc(2026, 3, 10, 0)); // 09:00 KR
        var alt = AltId.New();
        await svc.ToggleAsync(alt, "d", TestContext.Current.CancellationToken);
        time.Now = Utc(2026, 3, 10, 19, 59);
        Assert.Equal(ChecklistState.Done, (await svc.GetAsync(alt, TestContext.Current.CancellationToken))[0].State);
        time.Now = Utc(2026, 3, 10, 20, 0); // 05:00 KR dia 11
        var item = (await svc.GetAsync(alt, TestContext.Current.CancellationToken))[0];
        Assert.Equal(ChecklistState.Reset, item.State);
        Assert.Equal(0, item.Current);
    }

    [Fact]
    public async Task Weekly_ResetsWednesday0500Local()
    {
        var (svc, time) = Make(Utc(2026, 3, 10, 0)); // terca 09:00 KR
        var alt = AltId.New();
        await svc.SetCountAsync(alt, "w", 5, TestContext.Current.CancellationToken); // clamp para 2
        time.Now = Utc(2026, 3, 11, 0); // quarta 09:00 KR, depois do reset
        Assert.Equal(ChecklistState.Reset, (await svc.GetAsync(alt, TestContext.Current.CancellationToken))[1].State);
        await svc.SetCountAsync(alt, "w", 1, TestContext.Current.CancellationToken);
        time.Now = Utc(2026, 3, 17, 19, 0); // antes do proximo reset semanal
        Assert.Equal(1, (await svc.GetAsync(alt, TestContext.Current.CancellationToken))[1].Current);
    }

    private const string EventsJson = """
        { "events": [ {"id":"e","name":"E","days":["Saturday"],"times":["20:00","23:30"],"durationMinutes":60} ] }
        """;

    private static EventTimerService Timers(DateTimeOffset now) =>
        new(EventCatalog.Parse(EventsJson), ResetRule.For(ServerRegion.KR), new FakeTime(now));

    [Fact]
    public void Event_NextOccurrence_AndCountdown()
    {
        var t = Timers(Utc(2026, 3, 10, 0)).Get("e")!; // proximo sabado 14/3 20:00 KR = 11:00 UTC
        Assert.Equal(Utc(2026, 3, 14, 11), t.Start);
        Assert.False(t.IsActive);
        Assert.Equal(Utc(2026, 3, 14, 11) - Utc(2026, 3, 10, 0), t.Remaining);
    }

    [Fact]
    public void Event_Active_ThenNextSlot()
    {
        var active = Timers(Utc(2026, 3, 14, 11, 30)).Get("e")!;
        Assert.True(active.IsActive);
        Assert.Equal(TimeSpan.FromMinutes(30), active.Remaining);
        var next = Timers(Utc(2026, 3, 14, 12, 0)).Get("e")!; // fim do primeiro slot
        Assert.Equal(Utc(2026, 3, 14, 14, 30), next.Start);
    }

    [Fact]
    public void Event_CrossesMidnight_AndRollsToNextWeek()
    {
        Assert.True(Timers(Utc(2026, 3, 14, 15, 0)).Get("e")!.IsActive); // 00:00 KR domingo (slot 23:30 sab)
        Assert.Equal(Utc(2026, 3, 21, 11), Timers(Utc(2026, 3, 14, 15, 31)).Get("e")!.Start);
        Assert.Null(Timers(Utc(2026, 3, 10)).Get("nope"));
    }
}
