namespace Cloris.Aion2Flow.Domain.Progress;

/// <summary>Regra de recarga: <paramref name="Points"/> a cada <paramref name="Interval"/>.</summary>
public sealed record RechargeRule(string Key, TimeSpan Interval, int Points = 1)
{
    public const int DefaultMax = 560;
    public const int SubscriberMax = 840;
    public const int DefaultPoints = 15;
    public const int DefaultIntervalMinutes = 180;

    /// <summary>Padrao da resina: 15 pontos a cada 180 min.</summary>
    public static RechargeRule Default(string key) => new(key, TimeSpan.FromMinutes(DefaultIntervalMinutes), DefaultPoints);
}

/// <summary>Campos manuais da resina (por alt): intervalo (min), pontos por recarga e assinatura (0/1).</summary>
public static class ResinFields
{
    public const string Key = "resin";
    public const string Interval = "resin.interval";
    public const string Points = "resin.points";
    public const string Subscribed = "resin.subscribed";

    public static RechargeRule BuildRule(IEnumerable<ManualField> fields)
    {
        long interval = 0, points = 0;
        foreach (var f in fields)
        {
            if (f.Key == Interval) interval = f.Value;
            else if (f.Key == Points) points = f.Value;
        }

        return new RechargeRule(Key,
            TimeSpan.FromMinutes(interval > 0 ? interval : RechargeRule.DefaultIntervalMinutes),
            points > 0 ? (int)Math.Min(points, int.MaxValue) : RechargeRule.DefaultPoints);
    }

    public static bool IsSubscribed(IEnumerable<ManualField> fields) => fields.Any(f => f.Key == Subscribed && f.Value != 0);

    public static int MaxFor(bool subscribed) => subscribed ? RechargeRule.SubscriberMax : RechargeRule.DefaultMax;
}

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
public sealed class ResinMonitor
{
    private readonly IResourceRepository repo;
    private readonly IResinNotifier notifier;
    private readonly Func<AltId, RechargeRule> ruleFor;
    private readonly TimeProvider _time;

    public ResinMonitor(IResourceRepository repo, IResinNotifier notifier, RechargeRule rule, TimeProvider? time = null)
        : this(repo, notifier, _ => rule, time)
    {
    }

    /// <summary>Regra por alt (intervalo/pontos/assinatura sao campos manuais por personagem).</summary>
    public ResinMonitor(IResourceRepository repo, IResinNotifier notifier, Func<AltId, RechargeRule> ruleFor, TimeProvider? time = null)
    {
        this.repo = repo;
        this.notifier = notifier;
        this.ruleFor = ruleFor;
        _time = time ?? TimeProvider.System;
    }

    private readonly HashSet<(AltId, DateTimeOffset)> _notified = [];

    public async Task<int> CheckAsync(IEnumerable<AltId> alts, CancellationToken ct = default)
    {
        var now = _time.GetUtcNow();
        var count = 0;
        foreach (var alt in alts)
        {
            var rule = ruleFor(alt);
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
