using Staple;
using Staple.Internal;
using Staple.JoltPhysics;
using System.Numerics;

namespace CoreTests;

internal class RaycastTests
{
    [Test]
    public void TestBoxRaycast()
    {
        LayerMask.AllLayers.Add("Default");

        Physics3D.Instance = new Physics3D(new JoltPhysics3D());

        var ray = new Ray(new Vector3(0, 0, 1), new Vector3(0, 0, -1));

        Assert.That(Physics3D.Instance.CreateBox(default, Vector3.One * 2, Vector3.Zero, Quaternion.Identity, BodyMotionType.Dynamic, 0, false,
            0, 0, 0, false, false, false, false, 1, out var body), Is.True);

        Assert.That(Physics.RayCast3D(ray, out var target, out _, LayerMask.Everything), Is.True);

        Assert.That(target, Is.EqualTo(body));
    }

    [Test]
    public void TestRaycastFar()
    {
        LayerMask.AllLayers.Add("Default");

        Physics3D.Instance = new Physics3D(new JoltPhysics3D());

        var ray = new Ray(Vector3.Zero, new Vector3(0, 0, -1));

        Assert.That(Physics3D.Instance.CreateBox(default, Vector3.One * 2, new Vector3(0, 0, -10), Quaternion.Identity, BodyMotionType.Dynamic, 0, false,
            0, 0, 0, false, false, false, false, 1, out var body), Is.True);

        Assert.That(Physics.RayCast3D(ray, out var target, out _, LayerMask.Everything, maxDistance: 11), Is.True);

        Assert.That(target, Is.EqualTo(body));
    }

    [Test]
    public void TestBoxTriggerRaycast()
    {
        LayerMask.AllLayers.Add("Default");

        Physics3D.Instance = new Physics3D(new JoltPhysics3D());

        var ray = new Ray(new Vector3(0, 0, 1), new Vector3(0, 0, -1));

        Assert.That(Physics3D.Instance.CreateBox(default, Vector3.One * 2, Vector3.Zero, Quaternion.Identity, BodyMotionType.Dynamic, 0, true,
            0, 0, 0, false, false, false, false, 1, out var body), Is.True);

        Assert.That(body.IsTrigger, Is.True);

        Assert.That(Physics.RayCast3D(ray, out _, out _, LayerMask.Everything), Is.False);
    }

    [Test]
    public void TestBoxTriggerRaycastQuery()
    {
        LayerMask.AllLayers.Add("Default");

        Physics3D.Instance = new Physics3D(new JoltPhysics3D());

        var ray = new Ray(new Vector3(0, 0, 1), new Vector3(0, 0, -1));

        Assert.That(Physics3D.Instance.CreateBox(default, Vector3.One * 2, Vector3.Zero, Quaternion.Identity, BodyMotionType.Dynamic, 0, true,
            0, 0, 0, false, false, false, false, 1, out var body), Is.True);

        Assert.That(body.IsTrigger, Is.True);

        Assert.That(Physics.RayCast3D(ray, out _, out _, LayerMask.Everything, PhysicsTriggerQuery.Collide), Is.True);
    }
}
