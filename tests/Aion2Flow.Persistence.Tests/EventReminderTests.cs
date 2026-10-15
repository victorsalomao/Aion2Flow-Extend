using Cloris.Aion2Flow.Domain.Progress;
using Xunit;

namespace Cloris.Aion2Flow.Persistence.Tests;

public class EventReminderTests
{
    private sealed class Timers(DateTimeOffset start, bool active) : IEventTimerService
    {
        private readonly EventDefinition _def = new("e", "Evento", null, null, 10);
        public EventTimer? Get(string eventId) => GetAll()[0];
        public IReadOnlyList<EventTimer> GetAll() => [new EventTimer(_def, start, start.AddMinutes(10), active, TimeSpan.Zero)];
    }

    private sealed class Sink : IEventSoonNotifier
    {
        public int Count;
        public ValueTask NotifyEventSoonAsync(EventSoonNotification n, CancellationToken ct = default) { Count++; return ValueTask.CompletedTask; }
    }

    private sealed class Time(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public async Task Notifies_once_within_five_minutes()
    {
        var t = DateTimeOffset.UnixEpoch;
        var sink = new Sink();
        var far = new EventReminderMonitor(new Timers(t.AddMinutes(6), false), sink, time: new Time(t));
        Assert.Equal(0, await far.CheckAsync());
        var near = new EventReminderMonitor(new Timers(t.AddMinutes(5), false), sink, time: new Time(t));
        Assert.Equal(1, await near.CheckAsync());
        Assert.Equal(0, await near.CheckAsync());
        Assert.Equal(1, sink.Count);
    }
}
