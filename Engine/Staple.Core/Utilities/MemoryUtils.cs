using System;

namespace Staple.Internal;

internal static class MemoryUtils
{
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
