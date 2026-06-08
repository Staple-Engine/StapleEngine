using Staple;

namespace CoreTests;

internal class ComponentTests
{
    [Test]
    public void TestAddGet()
    {
        World.Current = new();

        var entity = Entity.Create();

        entity.AddComponent<Transform>();

        var transform = entity.GetComponent<Transform>();

        Assert.That(transform, Is.Not.Null);

        Assert.That(transform.Entity.IsValid, Is.True);

        Assert.That(entity.TryGetComponent(out transform), Is.True);

        Assert.That(entity.TryGetComponent(typeof(Transform), out var t), Is.True);

        World.Current = null;
    }

    [Test]
    public void TestAddSet()
    {
        World.Current = new();

        var entity = Entity.Create();

        var transform = new Transform();

        entity.SetComponent(transform);

        Assert.That(transform, Is.Not.Null);

        Assert.That(transform.Entity.IsValid, Is.True);

        World.Current = null;
    }

    [Test]
    public void TestAddRemove()
    {
        World.Current = new();

        var entity = Entity.Create(typeof(Transform));

        var transform = entity.GetComponent<Transform>();

        Assert.That(transform, Is.Not.Null);

        Assert.That(transform.Entity.IsValid, Is.True);

        entity.RemoveComponent<Transform>();

        transform = entity.GetComponent<Transform>();

        Assert.That(transform, Is.Not.Null);

        transform = entity.AddComponent<Transform>();

        World.Current.StartFrame();

        Assert.That(entity.GetComponent<Transform>(), Is.Not.Null);

        World.Current = null;
    }

    [Test]
    public void TestAddRemoveSet()
    {
        World.Current = new();

        var entity = Entity.Create(typeof(Transform));

        var transform = entity.GetComponent<Transform>();

        Assert.That(transform, Is.Not.Null);

        Assert.That(transform.Entity.IsValid, Is.True);

        entity.RemoveComponent<Transform>();

        transform = entity.GetComponent<Transform>();

        Assert.That(transform, Is.Not.Null);

        transform = new();

        entity.SetComponent(transform);

        World.Current.StartFrame();

        Assert.That(entity.GetComponent<Transform>(), Is.Not.Null);

        World.Current = null;
    }
}
