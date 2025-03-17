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

        Assert.IsNotNull(transform);

        Assert.IsTrue(transform.entity.IsValid);

        Assert.IsTrue(entity.TryGetComponent(out transform));

        Assert.IsTrue(entity.TryGetComponent(typeof(Transform), out var t));

        World.Current = null;
    }

    [Test]
    public void TestAddSet()
    {
        World.Current = new();

        var entity = Entity.Create();

        var transform = new Transform();

        entity.SetComponent(transform);

        Assert.IsNotNull(transform);

        Assert.IsTrue(transform.entity.IsValid);

        World.Current = null;
    }

    [Test]
    public void TestAddRemove()
    {
        World.Current = new();

        var entity = Entity.Create(typeof(Transform));

        var transform = entity.GetComponent<Transform>();

        Assert.IsNotNull(transform);

        Assert.IsTrue(transform.entity.IsValid);

        entity.RemoveComponent<Transform>();

        transform = entity.GetComponent<Transform>();

        Assert.IsNotNull(transform);

        transform = entity.AddComponent<Transform>();

        World.Current.StartFrame();

        Assert.IsNotNull(entity.GetComponent<Transform>());

        World.Current = null;
    }

    [Test]
    public void TestAddRemoveSet()
    {
        World.Current = new();

        var entity = Entity.Create(typeof(Transform));

        var transform = entity.GetComponent<Transform>();

        Assert.IsNotNull(transform);

        Assert.IsTrue(transform.entity.IsValid);

        entity.RemoveComponent<Transform>();

        transform = entity.GetComponent<Transform>();

        Assert.IsNotNull(transform);

        transform = new();

        entity.SetComponent(transform);

        World.Current.StartFrame();

        Assert.IsNotNull(entity.GetComponent<Transform>());

        World.Current = null;
    }
}
