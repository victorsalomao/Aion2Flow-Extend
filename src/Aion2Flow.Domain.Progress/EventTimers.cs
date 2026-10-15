using System.Globalization;

namespace Cloris.Aion2Flow.Domain.Progress;

/// <summary>Proxima ocorrencia (ou ocorrencia em andamento) de um evento.</summary>
public sealed record EventTimer(EventDefinition Event, DateTimeOffset Start, DateTimeOffset End, bool IsActive, TimeSpan Remaining);

public interface IEventTimerService
{
    EventTimer? Get(string eventId);

    /// <summary>Todos os eventos ordenados pelo que acontece primeiro (ativos antes).</summary>
    IReadOnlyList<EventTimer> GetAll();
}

public sealed class EventTimerService : IEventTimerService
{
    private readonly IEventCatalog _catalog;
    private readonly TimeProvider _time;
    private readonly TimeZoneInfo _zone;

    public EventTimerService(IEventCatalog catalog, ResetRule rule, TimeProvider? time = null)
    {
        _catalog = catalog;
        _time = time ?? TimeProvider.System;
        _zone = TimeZoneInfo.FindSystemTimeZoneById(rule.TimeZoneId);
    }

    public EventTimer? Get(string eventId)
    {
        var e = _catalog.Events.FirstOrDefault(x => x.Id == eventId);
        return e is null ? null : Compute(e, _time.GetUtcNow());
    }

    public IReadOnlyList<EventTimer> GetAll()
    {
        var now = _time.GetUtcNow();
        return _catalog.Events.Select(e => Compute(e, now)).OrderBy(t => t.Start).ToList();
    }

    private EventTimer Compute(EventDefinition e, DateTimeOffset now)
    {
        var localToday = TimeZoneInfo.ConvertTime(now, _zone).Date;
        var times = e.Times!.Select(s => TimeSpan.Parse(s, CultureInfo.InvariantCulture)).Order().ToList();
        var dur = TimeSpan.FromMinutes(e.DurationMinutes);
        // Comeca 1 dia antes para pegar ocorrencias que atravessam a meia-noite.
        for (var d = -1; d <= 8; d++)
        {
            var date = localToday.AddDays(d);
            if (!e.Days!.Contains(date.DayOfWeek)) continue;
            foreach (var t in times)
            {
                var start = ToOffset(date + t);
                var end = start + dur;
                if (end <= now) continue;
                return new(e, start, end, start <= now, start <= now ? end - now : start - now);
            }
        }
        throw new InvalidOperationException("Evento sem ocorrencia valida.");
    }

    private DateTimeOffset ToOffset(DateTime local)
    {
        var l = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (_zone.IsInvalidTime(l)) l = l.AddHours(1);
        return new DateTimeOffset(l, _zone.GetUtcOffset(l));
    }
}
