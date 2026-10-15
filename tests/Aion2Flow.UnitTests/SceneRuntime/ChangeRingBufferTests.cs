using Cloris.Aion2Flow.SceneRuntime.Stores;

namespace Cloris.Aion2Flow.UnitTests.SceneRuntime;

public sealed class ChangeRingBufferTests
{
    [Fact]
    public void Overflow_KeepsNewestAndDropsOldest()
    {
        var ring = new ChangeRingBuffer<int>(4, 4);
        for (var i = 1; i <= 10; i++)
            ring.Add(i);

        Assert.Equal(4, ring.Count);
        Assert.Equal(4, ring.Capacity);
        Assert.Equal(new[] { 7, 8, 9, 10 }, ring.GetRange(0, 4));
        Assert.Equal(9, ring.GetRange(1, 2)[1]);
    }

    [Fact]
    public void Unbounded_GrowsLikeList()
    {
        var ring = new ChangeRingBuffer<int>();
        for (var i = 0; i < 100; i++)
            ring.Add(i);
        Assert.Equal(100, ring.Count);
        Assert.Equal(99, ring[99]);
    }

    [Fact]
    public void Add_WhenFull_DoesNotAllocate()
    {
        var ring = new ChangeRingBuffer<long>(64, 64);
        for (var i = 0; i < 64; i++)
            ring.Add(i);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
            ring.Add(i);
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void Clear_ResetsCount()
    {
        var ring = new ChangeRingBuffer<int>(2, 2);
        ring.Add(1); ring.Add(2); ring.Add(3);
        ring.Clear();
        Assert.Equal(0, ring.Count);
        ring.Add(9);
        Assert.Equal(9, ring[0]);
    }
}
