using System;
using System.Collections;
using System.Collections.Generic;

namespace Staple;

/// <summary>
/// Expandable array that is thread-safe except for getting/setting values
/// Unlike typical arrays or lists, it keeps its maximum size, reducing allocations
/// </summary>
/// <typeparam name="T">The type to use</typeparam>
public class ConcurrentExpandableArray<T> : IEnumerable<T>
{
    private T[] list = [];
    private int length;
    private readonly object lockObject = new();

    public int Length
    {
        get => length;

        set
        {
            lock(lockObject)
            {
                length = value;

                if (length > list.Length)
                {
                    Array.Resize(ref list, length);
                }
            }
        }
    }

    public T this[int index]
    {
        get => list[index];

        set => list[index] = value;
    }

    public IEnumerator<T> GetEnumerator()
    {
        var count = 0;

        lock (lockObject)
        {
            count = length;

            for (var i = 0; i < count; i++)
            {
                yield return list[i];
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        var count = 0;

        lock (lockObject)
        {
            count = length;

            for (var i = 0; i < count; i++)
            {
                yield return list[i];
            }
        }
    }
}
