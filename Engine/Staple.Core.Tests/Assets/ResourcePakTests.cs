using Staple;
using Staple.Internal;
using Staple.Jobs;

namespace CoreTests;

internal unsafe class ResourcePakTests
{
    [Test]
    public void TestSerialize()
    {
        MessagePackInit.Initialize();

        StapleCodeGeneration.TypeCacheRegistration.RegisterAll();

        var container = new MemoryStream();

        var asset1 = new MemoryStream();

        var asset2 = new MemoryStream();

        var asset3 = new MemoryStream();

        int[] data1 = [1, 2, 3];

        int[] data2 = [123, 456, 789, 123];

        int[] data3 = [12345, 67890, 12345, 67890, 12345];

        {
            using var writer = new BinaryWriter(asset1);

            var buffer = new byte[data1.Length * sizeof(int)];

            fixed(void *ptr = data1)
            {
                new Span<byte>(ptr, buffer.Length).CopyTo(buffer.AsSpan());
            }

            writer.Write(buffer);
        }

        {
            using var writer = new BinaryWriter(asset2);

            var buffer = new byte[data2.Length * sizeof(int)];

            fixed (void* ptr = data2)
            {
                new Span<byte>(ptr, buffer.Length).CopyTo(buffer.AsSpan());
            }

            writer.Write(buffer);
        }

        {
            using var writer = new BinaryWriter(asset3);

            var buffer = new byte[data3.Length * sizeof(int)];

            fixed (void* ptr = data3)
            {
                new Span<byte>(ptr, buffer.Length).CopyTo(buffer.AsSpan());
            }

            writer.Write(buffer);
        }

        var pak = new ResourcePak();

        pak.AddEntry("2f38e460-7d44-47e7-a52e-6fee57f51bea", "asset1", "Staple.Data1", new MemoryStream(asset1.ToArray()));
        pak.AddEntry("afeb29a5-bfed-4c69-9ef4-ce1816b5d3ae", "asset2", "Staple.Data2", new MemoryStream(asset2.ToArray()));
        pak.AddEntry("7cf7b702-205d-43e1-9340-f9ce050a0692", "asset3", "Staple.Data3", new MemoryStream(asset3.ToArray()));

        Assert.That(pak.Serialize(container), Is.True);

        var readPak = new ResourcePak();

        var backend = new MemoryStream(container.ToArray());

        Assert.That(readPak.Deserialize(backend), Is.True);

        Assert.That(readPak.FileCount, Is.EqualTo(3));

        Assert.That(readPak.Files[0].guid, Is.EqualTo("2f38e460-7d44-47e7-a52e-6fee57f51bea"));
        Assert.That(readPak.Files[0].path, Is.EqualTo("asset1"));
        Assert.That(readPak.Files[0].typeName, Is.EqualTo("Staple.Data1"));

        Assert.That(readPak.Files[1].guid, Is.EqualTo("afeb29a5-bfed-4c69-9ef4-ce1816b5d3ae"));
        Assert.That(readPak.Files[1].path, Is.EqualTo("asset2"));
        Assert.That(readPak.Files[1].typeName, Is.EqualTo("Staple.Data2"));

        Assert.That(readPak.Files[2].guid, Is.EqualTo("7cf7b702-205d-43e1-9340-f9ce050a0692"));
        Assert.That(readPak.Files[2].path, Is.EqualTo("asset3"));
        Assert.That(readPak.Files[2].typeName, Is.EqualTo("Staple.Data3"));
    }

    [Test]
    public void TestAsyncRead()
    {
        MessagePackInit.Initialize();

        StapleCodeGeneration.TypeCacheRegistration.RegisterAll();

        var container = new MemoryStream();

        var asset1 = new MemoryStream();

        var asset2 = new MemoryStream();

        var asset3 = new MemoryStream();

        int[] data1 = [1, 2, 3];

        int[] data2 = [123, 456, 789, 123];

        int[] data3 = [12345, 67890, 12345, 67890, 12345];

        var data1Buffer = new byte[data1.Length * sizeof(int)];
        var data2Buffer = new byte[data2.Length * sizeof(int)];
        var data3Buffer = new byte[data3.Length * sizeof(int)];

        {
            using var writer = new BinaryWriter(asset1);

            fixed (void* ptr = data1)
            {
                new Span<byte>(ptr, data1Buffer.Length).CopyTo(data1Buffer.AsSpan());
            }

            writer.Write(data1Buffer);
        }

        {
            using var writer = new BinaryWriter(asset2);

            fixed (void* ptr = data2Buffer)
            {
                new Span<byte>(ptr, data2Buffer.Length).CopyTo(data2Buffer.AsSpan());
            }

            writer.Write(data2Buffer);
        }

        {
            using var writer = new BinaryWriter(asset3);

            fixed (void* ptr = data3)
            {
                new Span<byte>(ptr, data3Buffer.Length).CopyTo(data3Buffer.AsSpan());
            }

            writer.Write(data3Buffer);
        }

        var pak = new ResourcePak();

        pak.AddEntry("2f38e460-7d44-47e7-a52e-6fee57f51bea", "asset1", "Staple.Data1", new MemoryStream(asset1.ToArray()));
        pak.AddEntry("afeb29a5-bfed-4c69-9ef4-ce1816b5d3ae", "asset2", "Staple.Data2", new MemoryStream(asset2.ToArray()));
        pak.AddEntry("7cf7b702-205d-43e1-9340-f9ce050a0692", "asset3", "Staple.Data3", new MemoryStream(asset3.ToArray()));

        Assert.That(pak.Serialize(container), Is.True);

        var readPak = new ResourcePak();

        var backend = new MemoryStream(container.ToArray());

        Assert.That(readPak.Deserialize(backend), Is.True);

        var guids = new string[]
        {
            "2f38e460-7d44-47e7-a52e-6fee57f51bea",
            "afeb29a5-bfed-4c69-9ef4-ce1816b5d3ae",
            "7cf7b702-205d-43e1-9340-f9ce050a0692",
        };

        byte[][] buffers = [
            data1Buffer,
            data2Buffer,
            data3Buffer,
        ];

        var failed = false;

        var failLock = new Lock();

        void Handle(int index)
        {
            var guid = guids[index];

            var reader = readPak.OpenGuid(guid);

            var targetBuffer = buffers[index];

            var buffer = new byte[1024];

            var count = reader.Read(buffer);

            Assert.That(count, Is.EqualTo(targetBuffer.Length));

            Assert.That(buffer.AsSpan().Slice(0, count).SequenceEqual(targetBuffer), Is.True);
        }

        var actions = new Action[]
        {
            () => Handle(0),
            () => Handle(1),
            () => Handle(2),
        };

        var indices = new List<int>();

        for(var i = 0; i < 10000000; i++)
        {
            indices.Add(Random.Shared.Next(0, actions.Length));
        }

        var handles = new List<JobHandle>();

        foreach(var index in indices)
        {
            handles.Add(JobScheduler.Schedule(new ActionJob(actions[index],
                (e) =>
                {
                    Log.Error($"Failed to execute a job: {e}");

                    lock(failLock)
                    {
                        failed = true;
                    }
                })));
        }

        JobHandle.Complete(handles);

        Assert.That(failed, Is.False);
    }
}
