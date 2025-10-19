using Staple;

namespace CoreTests;

internal class ColorTests
{
    [Test]
    public void TestColorEquality()
    {
        var color = new Color(1, 2, 3, 4);

        Assert.That(color, Is.EqualTo(color));
        Assert.That(color, Is.Not.EqualTo(new Color(1, 2, 3, 0)));
        Assert.That(color, Is.Not.EqualTo(new Color(1, 2, 0, 4)));
        Assert.That(color, Is.Not.EqualTo(new Color(1, 0, 3, 4)));
        Assert.That(color, Is.Not.EqualTo(new Color(0, 2, 3, 4)));

        var color2 = new Color32(1, 2, 3, 4);

        Assert.That(color2, Is.EqualTo(color2));

        Assert.That(color2, Is.Not.EqualTo(new Color32(1, 2, 3, 0)));
        Assert.That(color2, Is.Not.EqualTo(new Color32(1, 2, 0, 4)));
        Assert.That(color2, Is.Not.EqualTo(new Color32(1, 0, 3, 4)));
        Assert.That(color2, Is.Not.EqualTo(new Color32(0, 2, 3, 4)));
    }

    [Test]
    public void TestColorConversion()
    {
        var a = new Color(0.5f, 0.25f, 1.0f, 0.0f);

        var b = (Color32)a;

        Assert.That(b.r, Is.EqualTo(127));
        Assert.That(b.g, Is.EqualTo(63));
        Assert.That(b.b, Is.EqualTo(255));
        Assert.That(b.a, Is.EqualTo(0));

        a = (Color)b;

        Assert.That(498, Is.EqualTo((int)(a.r * 1000)));
        Assert.That(247, Is.EqualTo((int)(a.g * 1000)));
        Assert.That(1, Is.EqualTo(a.b));
        Assert.That(0, Is.EqualTo(a.a));
    }

    [Test]
    public void TestColorIntValue()
    {
        var a = new Color(0.5f, 0.25f, 1.0f, 0.0f);

        var uintValue = a.UIntValue;

        Assert.That(uintValue, Is.EqualTo(0x7F3FFF00));

        var b = new Color32(255, 128, 64, 0);

        uintValue = b.UIntValue;

        Assert.That(uintValue, Is.EqualTo(0xFF804000));
    }
}
