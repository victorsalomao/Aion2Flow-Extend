namespace Cloris.Aion2Flow.Domain.Progress;

public sealed record SeasonProjection(long Missing, int DaysLeft, long PerDayNeeded, bool Reached, bool Ended);

public static class SeasonCalculator
{
    /// <summary>Pontos por dia necessarios; <paramref name="daysLeft"/> inclui o dia atual.</summary>
    public static SeasonProjection Compute(long current, long target, int daysLeft)
    {
        var missing = Math.Max(0, target - current);
        if (missing == 0) return new(0, Math.Max(0, daysLeft), 0, true, daysLeft <= 0);
        if (daysLeft <= 0) return new(missing, 0, missing, false, true);
        return new(missing, daysLeft, (missing + daysLeft - 1) / daysLeft, false, false);
    }
}
