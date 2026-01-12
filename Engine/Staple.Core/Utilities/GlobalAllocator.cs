using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Staple.Utilities;

public class GlobalAllocator<T> where T: unmanaged
{
    public sealed class GlobalAllocatorHandle(T[] contents) : IDisposable
    {
        private bool disposed = false;

        public T[] contents = contents;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            Instance.Return(contents);
        }
    }

    private class Entry
    {
        public T[] contents;
        public bool used;
    }
    
    private struct EntryPair
    {
        public List<Entry> entries;
        public Queue<Entry> freeEntries;
    }

    private readonly Dictionary<int, EntryPair> entries = [];

    public static readonly GlobalAllocator<T> Instance = new();

    public T[] Rent(int length)
    {
        if(length <= 0)
        {
            throw new ArgumentException($"Invalid length {length}", nameof(length));
        }

        ref var pair = ref CollectionsMarshal.GetValueRefOrAddDefault(entries, length, out var exists);

        if(exists == false)
        {
            pair.entries = [];
            pair.freeEntries = [];

            var contents = new T[length];

            pair.entries.Add(new()
            {
                contents = contents,
                used = true,
            });

            return contents;
        }

        if(pair.freeEntries.Count > 0)
        {
            var entry = pair.freeEntries.Dequeue();

            entry.used = true;

            return entry.contents;
        }

        {
            var contents = new T[length];

            pair.entries.Add(new()
            {
                contents = contents,
                used = true,
            });

            return contents;
        }
    }

    public void Return(T[] contents)
    {
        if (contents == null)
        {
            return;
        }

        ref var pair = ref CollectionsMarshal.GetValueRefOrAddDefault(entries, contents.Length, out var exists);

        if(exists == false)
        {
            return;
        }

        var span = CollectionsMarshal.AsSpan(pair.entries);
        var length = span.Length;

        for(var i = 0; i < length; i++)
        {
            ref var entry = ref span[i];

            if (entry.contents != contents)
            {
                continue;
            }

            entry.used = false;

            pair.freeEntries.Enqueue(entry);

            return;
        }
    }
}
