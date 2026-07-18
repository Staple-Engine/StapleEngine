using MessagePack;
using System;
using System.Runtime;

namespace Staple.Internal;

internal static class MemoryUtils
{
    public static void GarbageCollect(bool force)
    {
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

        MessagePackSerializer.DefaultOptions.SequencePool.Clear();

        GC.Collect(2, force ? GCCollectionMode.Forced : GCCollectionMode.Default, force, force);
    }

    public static T[] SafeCloneArray<T>(T[] source)
    {
        if((source?.Length ?? 0) == 0)
        {
            return [];
        }

        var outValue = new T[source.Length];

        Array.Copy(source, outValue, source.Length);

        return outValue;
    }

    public static T[] SafeCloneUnmanagedArray<T>(T[] source) where T: unmanaged
    {
        if ((source?.Length ?? 0) == 0)
        {
            return [];
        }

        var outValue = new T[source.Length];

        source.AsSpan().CopyTo(outValue.AsSpan());

        return outValue;
    }
}
