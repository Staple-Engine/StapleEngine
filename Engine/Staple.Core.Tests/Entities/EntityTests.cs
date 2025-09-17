using Staple;

namespace CoreTests;

internal class EntityTests
{
    [Test]
    public void TestCreateEntity()
    {
        World.Current = new();

        var entity = Entity.Create(typeof(Transform));

        Assert.IsTrue(entity.IsValid);

        World.Current = null;

        Assert.IsFalse(entity.IsValid);
    }

    [Test]
    public void TestDestroyEntity()
    {
        World.Current = new();

        var entity = Entity.Create(typeof(Transform));

        Assert.IsTrue(entity.IsValid);

        entity.Destroy();

        Assert.IsTrue(entity.IsValid);

        World.Current.StartFrame();

        Assert.IsFalse(entity.IsValid);

        World.Current = null;
    }

    [Test]
    public void TestAutoAssignEntity()
    {
        World.Current = new();

        var entity = Entity.Create(typeof(Transform));

        var transform = entity.GetComponent<Transform>();

        Assert.IsNotNull(transform);

        Assert.IsTrue(transform.Entity.IsValid);

        entity.RemoveComponent<Transform>();

        transform = entity.GetComponent<Transform>();

        Assert.IsNotNull(transform);

        transform = new();

        Assert.IsFalse(transform.Entity.IsValid);

        entity.SetComponent(transform);

        Assert.IsTrue(transform.Entity.IsValid);

        World.Current = null;
    }
}
