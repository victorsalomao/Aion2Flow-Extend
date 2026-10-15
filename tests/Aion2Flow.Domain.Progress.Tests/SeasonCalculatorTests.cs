using Xunit;

namespace Cloris.Aion2Flow.Domain.Progress.Tests;

public class SeasonCalculatorTests
{
    [Fact]
    public void PerDay_RoundsUp() => Assert.Equal(34, SeasonCalculator.Compute(0, 100, 3).PerDayNeeded);

    [Fact]
    public void Reached_NeedsNothing() => Assert.True(SeasonCalculator.Compute(100, 100, 3).Reached);

    [Fact]
    public void Ended_ReportsMissing()
    {
        var p = SeasonCalculator.Compute(10, 100, 0);
        Assert.True(p.Ended);
        Assert.Equal(90, p.Missing);
    }
}
