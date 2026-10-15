namespace Cloris.Aion2Flow.Domain.Progress;

public enum ServerRegion { KR, TW, NA, EU }

/// <summary>Regra de reset: hora local do servidor e dia da semana do reset semanal.</summary>
public sealed record ResetRule(string TimeZoneId, TimeSpan DailyTime, DayOfWeek WeeklyDay)
{
    public static ResetRule For(ServerRegion region) => region switch
    {
        ServerRegion.KR => new("Asia/Seoul", TimeSpan.FromHours(5), DayOfWeek.Wednesday),
        ServerRegion.TW => new("Asia/Taipei", TimeSpan.FromHours(5), DayOfWeek.Wednesday),
        ServerRegion.NA => new("America/Los_Angeles", TimeSpan.FromHours(5), DayOfWeek.Wednesday),
        ServerRegion.EU => new("Europe/Berlin", TimeSpan.FromHours(5), DayOfWeek.Wednesday),
        _ => throw new ArgumentOutOfRangeException(nameof(region)),
    };
}

public interface IResetClock
{
    DateTimeOffset NextDaily();
    DateTimeOffset NextWeekly();
}

/// <summary>Calcula os proximos resets no fuso do servidor; nunca usa o fuso local.</summary>
public sealed class ResetClock : IResetClock
{
    private readonly TimeProvider _time;
    private readonly TimeZoneInfo _zone;
    private readonly ResetRule _rule;

    public ResetClock(ServerRegion region, TimeProvider? time = null)
        : this(ResetRule.For(region), time) { }

    public ResetClock(ResetRule rule, TimeProvider? time = null)
    {
        _rule = rule;
        _time = time ?? TimeProvider.System;
        _zone = TimeZoneInfo.FindSystemTimeZoneById(rule.TimeZoneId);
    }

    public DateTimeOffset NextDaily() => Next(0);

    public DateTimeOffset NextWeekly() => Next((int)_rule.WeeklyDay + 1);

    // weekDay = 0: diario; senao dia da semana + 1.
    private DateTimeOffset Next(int weekDay)
    {
        var now = _time.GetUtcNow();
        var local = TimeZoneInfo.ConvertTime(now, _zone).DateTime;
        var date = local.Date;
        if (weekDay != 0)
            date = date.AddDays(((weekDay - 1 - (int)date.DayOfWeek) + 7) % 7);
        var step = weekDay == 0 ? 1 : 7;
        var cand = ToOffset(date + _rule.DailyTime);
        while (cand <= now)
        {
            date = date.AddDays(step);
            cand = ToOffset(date + _rule.DailyTime);
        }
        return cand;
    }

    private DateTimeOffset ToOffset(DateTime unspecifiedLocal)
    {
        var l = DateTime.SpecifyKind(unspecifiedLocal, DateTimeKind.Unspecified);
        if (_zone.IsInvalidTime(l)) l = l.AddHours(1); // lacuna de DST
        return new DateTimeOffset(l, _zone.GetUtcOffset(l));
    }
}
