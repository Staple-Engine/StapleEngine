using MessagePack;
using System;
using System.Runtime;

namespace Staple.Internal;

public static class MemoryUtils
{
    public static void GarbageCollect(bool force)
    {
        MessagePackSerializer.DefaultOptions.SequencePool.Clear();

        GC.Collect(2, GCCollectionMode.Default, force, force);
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

    public static byte[] ToByteArray<T>(ReadOnlySpan<T> contents) where T : unmanaged
    {
        var byteSize = TypeCache.SizeOf(typeof(T).ToString()) * contents.Length;

        var buffer = new byte[byteSize];

        unsafe
        {
            fixed(void *ptr = buffer)
            {
                contents.CopyTo(new Span<T>(ptr, contents.Length));
            }
        }

        return buffer;
    }

    public static void FillArrayWithBytes<T>(byte[] data, Array target) where T: unmanaged
    {
        if(target is not T[] localArray)
        {
            return;
        }

        var byteSize = TypeCache.SizeOf(typeof(T).ToString());

        if (data.Length % byteSize != 0)
        {
            return;
        }

        var elementCount = data.Length / byteSize;

        unsafe
        {
            fixed (byte* ptr = data)
            {
                new Span<T>(ptr, elementCount).CopyTo(localArray.AsSpan());
            }
        }

    }
}
