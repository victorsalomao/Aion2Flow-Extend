namespace Cloris.Aion2Flow.Domain.Progress;

public sealed record EventSoonNotification(string EventId, string Name, DateTimeOffset Start);

/// <summary>Implementada pela UI/shell; o dominio so conhece a interface.</summary>
public interface IEventSoonNotifier
{
    ValueTask NotifyEventSoonAsync(EventSoonNotification notification, CancellationToken ct = default);
}

/// <summary>Avisa uma unica vez por ocorrencia quando um evento comeca em ate <c>lead</c>.</summary>
public sealed class EventReminderMonitor(IEventTimerService timers, IEventSoonNotifier notifier, TimeSpan? lead = null, TimeProvider? time = null)
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly TimeSpan _lead = lead ?? TimeSpan.FromMinutes(5);
    private readonly HashSet<(string, DateTimeOffset)> _notified = [];

    public async Task<int> CheckAsync(CancellationToken ct = default)
    {
        var now = _time.GetUtcNow();
        var count = 0;
        foreach (var t in timers.GetAll())
        {
            var until = t.Start - now;
            if (t.IsActive || until <= TimeSpan.Zero || until > _lead || !_notified.Add((t.Event.Id, t.Start))) continue;
            await notifier.NotifyEventSoonAsync(new EventSoonNotification(t.Event.Id, t.Event.Name, t.Start), ct);
            count++;
        }

        return count;
    }
}
