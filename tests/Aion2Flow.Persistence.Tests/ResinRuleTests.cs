using Cloris.Aion2Flow.Domain.Progress;
using Xunit;

namespace Cloris.Aion2Flow.Persistence.Tests;

public class ResinRuleTests
{
    [Fact]
    public void Default_rule_is_15_points_every_180_min()
    {
        var rule = ResinFields.BuildRule([]);
        Assert.Equal(15, rule.Points);
        Assert.Equal(TimeSpan.FromMinutes(180), rule.Interval);
        Assert.Equal(560, ResinFields.MaxFor(false));
        Assert.Equal(840, ResinFields.MaxFor(true));
    }

    [Fact]
    public void Projection_and_full_time_use_points()
    {
        var t = DateTimeOffset.UnixEpoch;
        var rule = RechargeRule.Default("resin");
        var r = new Resource(AltId.New(), "resin", 500, 560, t);
        Assert.Equal(515, ResinCalculator.Project(r, rule, t.AddMinutes(185)));
        Assert.Equal(560, ResinCalculator.Project(r, rule, t.AddDays(2)));
        Assert.Equal(t.AddMinutes(180 * 4), ResinCalculator.FullAt(r, rule));
    }

    [Fact]
    public void Subscription_flag_persists_as_manual_field()
    {
        var a = AltId.New();
        var now = DateTimeOffset.UnixEpoch;
        var on = ManualField.Create(a, ResinFields.Subscribed, 1, now, now);
        Assert.True(ResinFields.IsSubscribed([on]));
        Assert.False(ResinFields.IsSubscribed([on with { Value = 0 }]));
    }
}
