using Staple.Utilities;
using System.Numerics;

namespace CoreTests;

internal class FreeformAllocatorTests
{
    [Test]
    public void TestAllocate()
    {
        var allocator = new FreeformAllocator<Vector2>();

        var entry = allocator.Allocate(20);

        Assert.That(entry.start, Is.EqualTo(0));
        Assert.That(entry.length, Is.EqualTo(20));

        var second = allocator.Allocate(30);

        Assert.That(second.start, Is.EqualTo(20));
        Assert.That(second.length, Is.EqualTo(30));

        Assert.That(allocator.buffer.Length, Is.EqualTo(50));

        var span = allocator.Get(entry);

        Assert.That(span.Length, Is.EqualTo(20));

        span = allocator.Get(second);

        Assert.That(span.Length, Is.EqualTo(30));
    }

    [Test]
    public void TestFree()
    {
        var allocator = new FreeformAllocator<Vector2>();

        var entry = allocator.Allocate(20);

        Assert.That(entry.start, Is.EqualTo(0));
        Assert.That(entry.length, Is.EqualTo(20));

        var second = allocator.Allocate(30);

        Assert.That(second.start, Is.EqualTo(20));
        Assert.That(second.length, Is.EqualTo(30));

        allocator.Free(entry);

        Assert.That(entry.freed, Is.True);

        Assert.That(allocator.freeEntries, Has.Count.EqualTo(1));

        var free = allocator.freeEntries.FirstOrDefault();

        Assert.That(free.start, Is.EqualTo(0));
        Assert.That(free.length, Is.EqualTo(20));
    }

    [Test]
    public void TestCompact()
    {
        var allocator = new FreeformAllocator<Vector2>();

        var entry = allocator.Allocate(20);

        Assert.That(entry.start, Is.EqualTo(0));
        Assert.That(entry.length, Is.EqualTo(20));

        var span = allocator.Get(entry);

        Assert.That(span.Length, Is.EqualTo(20));

        for(var i = 0; i < span.Length; i++)
        {
            span[i] = Vector2.One;
        }

        var second = allocator.Allocate(30);

        Assert.That(second.start, Is.EqualTo(20));
        Assert.That(second.length, Is.EqualTo(30));

        span = allocator.Get(second);

        Assert.That(span.Length, Is.EqualTo(30));

        for (var i = 0; i < span.Length; i++)
        {
            Assert.That(span[i], Is.EqualTo(Vector2.Zero));

            span[i] = new(0, 1);
        }

        allocator.Free(entry);

        Assert.That(allocator.freeEntries, Has.Count.EqualTo(1));

        var free = allocator.freeEntries.FirstOrDefault();

        Assert.That(free.start, Is.EqualTo(0));
        Assert.That(free.length, Is.EqualTo(20));

        entry = allocator.Allocate(10);

        Assert.That(entry.start, Is.EqualTo(0));
        Assert.That(entry.length, Is.EqualTo(10));

        span = allocator.Get(entry);

        Assert.That(span.Length, Is.EqualTo(10));

        for (var i = 0; i < span.Length; i++)
        {
            Assert.That(span[i], Is.EqualTo(Vector2.One));

            span[i] = Vector2.Zero;
        }

        var third = allocator.Allocate(20);

        Assert.That(entry.start, Is.EqualTo(0));
        Assert.That(entry.length, Is.EqualTo(10));

        Assert.That(second.start, Is.EqualTo(10));
        Assert.That(second.length, Is.EqualTo(30));

        Assert.That(third.start, Is.EqualTo(40));
        Assert.That(third.length, Is.EqualTo(20));

        span = allocator.Get(entry);

        for (var i = 0; i < span.Length; i++)
        {
            Assert.That(span[i], Is.EqualTo(Vector2.Zero));
        }

        Assert.That(span.Length, Is.EqualTo(10));

        span = allocator.Get(second);

        for (var i = 0; i < span.Length; i++)
        {
            Assert.That(span[i], Is.EqualTo(new Vector2(0, 1)));
        }

        Assert.That(span.Length, Is.EqualTo(30));

        span = allocator.Get(third);

        Assert.That(span.Length, Is.EqualTo(20));

        for (var i = 0; i < span.Length; i++)
        {
            Assert.That(span[i], Is.EqualTo(Vector2.Zero));
        }
    }
}
