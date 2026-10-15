namespace Cloris.Aion2Flow.Domain.Progress;

/// <summary>Regra de recarga: <paramref name="Points"/> a cada <paramref name="Interval"/>.</summary>
public sealed record RechargeRule(string Key, TimeSpan Interval, int Points = 1);

public static class ResinCalculator
{
    public static int Project(Resource r, RechargeRule rule, DateTimeOffset now)
    {
        if (r.Current >= r.Max || now <= r.UpdatedAt || rule.Interval <= TimeSpan.Zero) return r.Current;
        var ticks = (long)((now - r.UpdatedAt) / rule.Interval);
        return (int)Math.Min(r.Max, r.Current + ticks * rule.Points);
    }

    public static DateTimeOffset FullAt(Resource r, RechargeRule rule)
    {
        var missing = r.Max - r.Current;
        if (missing <= 0) return r.UpdatedAt;
        var ticks = (missing + rule.Points - 1) / rule.Points;
        return r.UpdatedAt + rule.Interval * ticks;
    }
}

public sealed record ResinFullNotification(AltId AltId, string Key, DateTimeOffset FullAt);

/// <summary>Implementada pela UI/shell (toast etc.); o dominio so conhece a interface.</summary>
public interface IResinNotifier
{
    ValueTask NotifyFullAsync(ResinFullNotification notification, CancellationToken ct = default);
}

/// <summary>Notifica uma unica vez por estado salvo quando a recarga completa.</summary>
public sealed class ResinMonitor(IResourceRepository repo, IResinNotifier notifier, RechargeRule rule, TimeProvider? time = null)
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly HashSet<(AltId, DateTimeOffset)> _notified = [];

    public async Task<int> CheckAsync(IEnumerable<AltId> alts, CancellationToken ct = default)
    {
        var now = _time.GetUtcNow();
        var count = 0;
        foreach (var alt in alts)
        {
            foreach (var r in await repo.ListByAltAsync(alt, ct))
            {
                if (r.Key != rule.Key) continue;
                var full = ResinCalculator.FullAt(r, rule);
                if (full > now || !_notified.Add((alt, r.UpdatedAt))) continue;
                await notifier.NotifyFullAsync(new ResinFullNotification(alt, r.Key, full), ct);
                count++;
            }
        }
        return count;
    }
}
