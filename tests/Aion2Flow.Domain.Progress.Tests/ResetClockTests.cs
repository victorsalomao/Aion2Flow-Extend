using Xunit;

namespace Cloris.Aion2Flow.Domain.Progress.Tests;

public class ResetClockTests
{
    private sealed class FakeTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static DateTimeOffset Utc(int y, int m, int d, int h = 0, int mi = 0) =>
        new(y, m, d, h, mi, 0, TimeSpan.Zero);

    private static ResetClock Clock(ServerRegion r, DateTimeOffset now) => new(r, new FakeTime(now));

    // KR = UTC+9, reset 05:00 local = 20:00 UTC do dia anterior.
    [Theory]
    [InlineData(2026, 3, 10, 0, 0, 2026, 3, 10, 20)]   // antes do reset
    [InlineData(2026, 3, 10, 19, 59, 2026, 3, 10, 20)] // 1 min antes
    [InlineData(2026, 3, 10, 20, 0, 2026, 3, 11, 20)]  // exatamente no reset: proximo
    [InlineData(2026, 3, 10, 23, 0, 2026, 3, 11, 20)]  // depois
    [InlineData(2026, 12, 31, 21, 0, 2027, 1, 1, 20)]  // virada de ano
    [InlineData(2026, 2, 28, 21, 0, 2026, 3, 1, 20)]   // virada de mes
    public void Kr_NextDaily(int y, int m, int d, int h, int mi, int ey, int em, int ed, int eh) =>
        Assert.Equal(Utc(ey, em, ed, eh), Clock(ServerRegion.KR, Utc(y, m, d, h, mi)).NextDaily());

    [Fact]
    public void Kr_NextWeekly_IsWednesday0500Local()
    {
        // 2026-03-10 (terca). Quarta 11/03 05:00 KST = 10/03 20:00 UTC.
        var next = Clock(ServerRegion.KR, Utc(2026, 3, 10, 0)).NextWeekly();
        Assert.Equal(Utc(2026, 3, 10, 20), next);
        Assert.Equal(DayOfWeek.Wednesday, next.ToOffset(TimeSpan.FromHours(9)).DayOfWeek);
    }

    [Fact]
    public void Kr_NextWeekly_AtReset_GoesToNextWeek() =>
        Assert.Equal(Utc(2026, 3, 17, 20), Clock(ServerRegion.KR, Utc(2026, 3, 10, 20)).NextWeekly());

    [Fact]
    public void Kr_NextWeekly_CrossesMonth() =>
        // quinta 26/03 -> quarta 01/04 05:00 KST = 31/03 20:00 UTC
        Assert.Equal(Utc(2026, 3, 31, 20), Clock(ServerRegion.KR, Utc(2026, 3, 26, 12)).NextWeekly());

    [Fact]
    public void Tw_NextDaily_Utc8() =>
        Assert.Equal(Utc(2026, 3, 9, 21), Clock(ServerRegion.TW, Utc(2026, 3, 9, 10)).NextDaily());

    [Fact]
    public void Na_NextDaily_Winter_Pst()
    {
        // PST UTC-8: 05:00 = 13:00 UTC
        Assert.Equal(Utc(2026, 1, 15, 13), Clock(ServerRegion.NA, Utc(2026, 1, 15, 0)).NextDaily());
    }

    [Fact]
    public void Na_NextDaily_Summer_Pdt() =>
        // PDT UTC-7: 05:00 = 12:00 UTC
        Assert.Equal(Utc(2026, 7, 15, 12), Clock(ServerRegion.NA, Utc(2026, 7, 15, 0)).NextDaily());

    [Fact]
    public void Na_Dst_SpringForward_OffsetChanges()
    {
        // DST NA 2026 comeca 08/03 02:00. Em 07/03 12:00 UTC, proximo reset 07/03 13:00 UTC (PST);
        // logo apos, o seguinte (08/03 05:00 PDT) = 12:00 UTC.
        var c1 = Clock(ServerRegion.NA, Utc(2026, 3, 7, 12)).NextDaily();
        Assert.Equal(Utc(2026, 3, 7, 13), c1);
        var c2 = Clock(ServerRegion.NA, c1).NextDaily();
        Assert.Equal(Utc(2026, 3, 8, 12), c2);
    }

    [Fact]
    public void Eu_Dst_FallBack_OffsetChanges()
    {
        // Berlin: CEST -> CET em 25/10/2026. 24/10 05:00 CEST = 03:00 UTC; 25/10 05:00 CET = 04:00 UTC.
        var c1 = Clock(ServerRegion.EU, Utc(2026, 10, 24, 0)).NextDaily();
        Assert.Equal(Utc(2026, 10, 24, 3), c1);
        Assert.Equal(Utc(2026, 10, 25, 4), Clock(ServerRegion.EU, c1).NextDaily());
    }

    [Fact]
    public void Eu_Weekly_AcrossDst_UsesLocalWallClock()
    {
        // quinta 22/10/2026 -> quarta 28/10 05:00 CET = 04:00 UTC
        Assert.Equal(Utc(2026, 10, 28, 4), Clock(ServerRegion.EU, Utc(2026, 10, 22, 12)).NextWeekly());
    }

    [Fact]
    public void Custom_Rule_InvalidLocalTime_ShiftsForward()
    {
        // 02:30 em 08/03/2026 nao existe em LA; desloca para 03:30 PDT = 10:30 UTC.
        var rule = new ResetRule("America/Los_Angeles", new TimeSpan(2, 30, 0), DayOfWeek.Sunday);
        var clock = new ResetClock(rule, new FakeTime(Utc(2026, 3, 8, 0)));
        Assert.Equal(Utc(2026, 3, 8, 10, 30), clock.NextDaily());
    }

    [Fact]
    public void Result_IndependentOfProcessLocalZone() =>
        Assert.Equal(TimeSpan.FromHours(9), Clock(ServerRegion.KR, Utc(2026, 3, 10)).NextDaily().Offset);
}
