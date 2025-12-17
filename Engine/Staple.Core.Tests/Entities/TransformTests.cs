using Staple;
using System.Numerics;

namespace CoreTests;

internal class TransformTests
{
    [Test]
    public void TestChanged()
    {
        var transform = new Transform();

        Assert.That(transform.Version, Is.EqualTo(0));

        transform.LocalPosition = transform.LocalPosition;

        Assert.That(transform.Version, Is.EqualTo(0));

        transform.LocalRotation = transform.LocalRotation;

        Assert.That(transform.Version, Is.EqualTo(0));

        transform.LocalScale = transform.LocalScale;

        Assert.That(transform.Version, Is.EqualTo(0));

        transform.Position = transform.Position;

        Assert.That(transform.Version, Is.EqualTo(0));

        transform.Rotation = transform.Rotation;

        Assert.That(transform.Version, Is.EqualTo(0));

        transform.Scale = transform.Scale;

        Assert.That(transform.Version, Is.EqualTo(0));

        transform.LocalPosition = Vector3.One;

        Assert.That(transform.Version, Is.EqualTo(1));

        transform.LocalRotation = Quaternion.CreateFromYawPitchRoll(1, 2, 3);

        Assert.That(transform.Version, Is.EqualTo(2));

        transform.LocalScale = Vector3.Zero;

        Assert.That(transform.Version, Is.EqualTo(3));

        transform.Position = Vector3.Zero;

        Assert.That(transform.Version, Is.EqualTo(4));

        transform.Rotation = Quaternion.Identity;

        Assert.That(transform.Version, Is.EqualTo(5));

        transform.Scale = Vector3.One;

        Assert.That(transform.Version, Is.EqualTo(6));
    }

    [Test]
    public void TestLocalPosition()
    {
        var transform = new Transform();

        Assert.That(transform.Version, Is.EqualTo(0));

        transform.LocalPosition = new Vector3(0, 0, 1);

        Assert.That(transform.Version, Is.EqualTo(1));

        var matrix = transform.Matrix;

        Assert.That(matrix, Is.Not.EqualTo(Matrix4x4.Identity));
    }

    [Test]
    public void TestLocalRotation()
    {
        var transform = new Transform();

        Assert.That(transform.Version, Is.EqualTo(0));

        transform.LocalRotation = Quaternion.Euler(new Vector3(0, 90, 0));

        Assert.That(transform.Version, Is.EqualTo(1));

        var matrix = transform.Matrix;

        Assert.That(matrix, Is.Not.EqualTo(Matrix4x4.Identity));

        var forward = transform.Forward;

        forward.X = Staple.Math.Round(forward.X);
        forward.Y = Staple.Math.Round(forward.Y);
        forward.Z = Staple.Math.Round(forward.Z);

        Assert.That(new Vector3(1, 0, 0), Is.EqualTo(forward));
    }

    [Test]
    public void TestLocalScale()
    {
        var transform = new Transform();

        Assert.That(transform.Version, Is.EqualTo(0));

        transform.LocalScale = Vector3.One * 0.5f;

        Assert.That(transform.Version, Is.EqualTo(1));

        var matrix = transform.Matrix;

        Assert.That(matrix, Is.Not.EqualTo(Matrix4x4.Identity));

        var scaled = Vector3.Transform(Vector3.One, matrix);

        Assert.That(new Vector3(0.5f, 0.5f, 0.5f), Is.EqualTo(scaled));
    }

    [Test]
    public void TestPosition()
    {
        var transform = new Transform();
        var parent = new Transform();

        transform.SetParent(parent);

        transform.LocalPosition = new Vector3(0, 0, 1);

        Assert.That(transform.Version, Is.EqualTo(2));

        Assert.That(transform.Position, Is.EqualTo(new Vector3(0, 0, 1)));

        parent.LocalPosition = new(0, 0, 1);

        Assert.That(transform.Position, Is.EqualTo(new Vector3(0, 0, 2)));

        transform.LocalPosition = Vector3.Zero;

        Assert.That(transform.Position, Is.EqualTo(new Vector3(0, 0, 1)));
    }

    [Test]
    public void TestRotation()
    {
        var transform = new Transform();
        var parent = new Transform();

        transform.SetParent(parent);

        transform.LocalRotation = Quaternion.Euler(new(0, 45, 0));

        Assert.That(transform.Version, Is.EqualTo(2));

        var angles = transform.Rotation.ToEulerAngles();

        angles.X = Staple.Math.Round(angles.X);
        angles.Y = Staple.Math.Round(angles.Y);
        angles.Z = Staple.Math.Round(angles.Z);

        Assert.That(angles, Is.EqualTo(new Vector3(0, 45, 0)));

        parent.LocalRotation = Quaternion.Euler(new(0, 45, 0));

        angles = transform.Rotation.ToEulerAngles();

        angles.X = Staple.Math.Round(angles.X);
        angles.Y = Staple.Math.Round(angles.Y);
        angles.Z = Staple.Math.Round(angles.Z);

        Assert.That(angles, Is.EqualTo(new Vector3(0, 90, 0)));

        transform.Rotation = Quaternion.Euler(new(0, 45, 0));

        angles = transform.Rotation.ToEulerAngles();

        angles.X = Staple.Math.Round(angles.X);
        angles.Y = Staple.Math.Round(angles.Y);
        angles.Z = Staple.Math.Round(angles.Z);

        Assert.That(angles, Is.EqualTo(new Vector3(0, 45, 0)));
    }

    [Test]
    public void TestScale()
    {
        var transform = new Transform();
        var parent = new Transform();

        transform.SetParent(parent);

        transform.LocalScale = new Vector3(2, 2, 2);

        Assert.That(transform.Version, Is.EqualTo(2));

        Assert.That(transform.Scale, Is.EqualTo(new Vector3(2, 2, 2)));

        parent.LocalScale = new(0.5f, 0.5f, 0.5f);

        Assert.That(transform.Scale, Is.EqualTo(new Vector3(1, 1, 1)));

        transform.Scale = new(1, 1, 1);

        Assert.That(transform.Scale, Is.EqualTo(new Vector3(1, 1, 1)));
    }
}