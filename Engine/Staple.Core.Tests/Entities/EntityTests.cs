using Staple;

namespace CoreTests;

internal class EntityTests
{
    [Test]
    public void TestCreateEntity()
    {
        World.Current = new();

        var entity = Entity.Create(typeof(Transform));

        Assert.That(entity.IsValid, Is.True);

        World.Current = null;

        Assert.That(entity.IsValid, Is.False);
    }

    [Test]
    public void TestDestroyEntity()
    {
        World.Current = new();

        var entity = Entity.Create(typeof(Transform));

        Assert.That(entity.IsValid, Is.True);

        entity.Destroy();

        Assert.That(entity.IsValid, Is.True);

        World.Current.StartFrame();

        Assert.That(entity.IsValid, Is.False);

        World.Current = null;
    }

    [Test]
    public void TestAutoAssignEntity()
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

        Assert.That(transform.Entity.IsValid, Is.False);

        entity.SetComponent(transform);

        Assert.That(transform.Entity.IsValid, Is.True);

        World.Current = null;
    }
}
