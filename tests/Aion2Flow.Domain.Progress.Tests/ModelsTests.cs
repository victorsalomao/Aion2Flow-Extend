using Xunit;

namespace Cloris.Aion2Flow.Domain.Progress.Tests;

public class ModelsTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Ids_AreUnique() => Assert.NotEqual(AltId.New(), AltId.New());

    [Fact]
    public void Alt_Rename_ReturnsCopy()
    {
        var a = new Alt(AltId.New(), "A", "S1", "Templar", 60);
        var b = a.Rename("B");
        Assert.Equal("A", a.Name);
        Assert.Equal("B", b.Name);
        Assert.Equal(a.Id, b.Id);
    }

    [Theory]
    [InlineData("x", 1, true)]
    [InlineData("", 1, false)]
    [InlineData("x", 0, false)]
    public void Task_IsValid(string name, int target, bool expected) =>
        Assert.Equal(expected, new ProgressTask(TaskId.New(), name, TaskCadence.Daily, target).IsValid);

    [Fact]
    public void TaskProgress_Advance_ClampsToTarget()
    {
        var p = new TaskProgress(AltId.New(), TaskId.New(), 1, Now);
        var n = p.Advance(10, 3, Now.AddHours(1));
        Assert.Equal(3, n.Current);
        Assert.True(n.IsComplete(3));
        Assert.False(p.IsComplete(3));
        Assert.Equal(Now.AddHours(1), n.UpdatedAt);
    }

    [Fact]
    public void Resource_SpendAndGain_AreBounded()
    {
        var r = new Resource(AltId.New(), "energy", 5, 10, Now);
        Assert.Equal(0, r.Spend(99, Now).Current);
        Assert.Equal(10, r.Gain(99, Now).Current);
        Assert.Equal(5, r.Current);
    }

    [Fact]
    public void Events_ShareBaseAndCarryData()
    {
        var alt = AltId.New();
        var t = TaskId.New();
        ProgressEvent e = new TaskCompleted(Guid.NewGuid(), Now, alt, t);
        Assert.Equal(alt, e.AltId);
        Assert.Equal(new TaskProgressed(e.Id, Now, alt, t, 2).AltId, e.AltId);
        Assert.Equal("k", new ResourceChanged(e.Id, Now, alt, "k", 1).Key);
    }
}
