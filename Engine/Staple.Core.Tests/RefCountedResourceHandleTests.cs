using Staple.Internal;

namespace CoreTests;

internal class RefCountedResourceHandleTests
{
    [Test]
    public void TestCreateFree()
    {
        var disposed = false;

        var handle = new RefCountedResourceHandle("test", 1, (key, content) =>
        {
            Assert.That(disposed, Is.False);

            disposed = true;
        });

        Assert.That(handle.IsValid, Is.True);
        Assert.That(handle.content, Is.EqualTo(1));
        Assert.That(handle.RefCount, Is.EqualTo(1));

        handle.Dispose();

        Assert.That(handle.IsValid, Is.False);
        Assert.That(handle.content, Is.EqualTo(1));
        Assert.That(disposed, Is.True);
    }

    [Test]
    public void TestMultipleRefs()
    {
        var disposed = false;
        var handles = new List<RefCountedResourceHandle>();

        for(var i = 0; i < 10; i++)
        {
            var handle = new RefCountedResourceHandle("test", 1, (key, content) =>
            {
                Assert.That(disposed, Is.False);

                disposed = true;
            });

            Assert.That(handle.IsValid, Is.True);
            Assert.That(handle.content, Is.EqualTo(1));
            Assert.That(handle.RefCount, Is.EqualTo(i + 1));

            Assert.That(disposed, Is.False);

            handles.Add(handle);
        }

        for(var i = 0; i < handles.Count; i++)
        {
            Assert.That(disposed, Is.False);

            handles[i].Dispose();

            if (i == handles.Count - 1)
            {
                for (var j = 0; j < handles.Count; j++)
                {
                    Assert.That(handles[j].RefCount, Is.EqualTo(0));
                    Assert.That(handles[j].IsValid, Is.False);
                    Assert.That(disposed, Is.True);
                }
            }
            else
            {
                for (var j = 0; j < handles.Count; j++)
                {
                    Assert.That(handles[j].RefCount, Is.EqualTo(10 - i - 1));
                    Assert.That(handles[j].IsValid, Is.EqualTo(j > i));
                }
            }
        }
    }
}
