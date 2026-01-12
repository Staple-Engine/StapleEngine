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
    
    private class EntryPair
    {
        public readonly List<Entry> entries = [];
        public readonly List<Entry> freeEntries = [];
    }

    private readonly Dictionary<int, EntryPair> entries = [];

    public static readonly GlobalAllocator<T> Instance = new();

    public T[] Rent(int length)
    {
        if(length <= 0)
        {
            throw new ArgumentException($"Invalid length {length}", nameof(length));
        }

        if(entries.TryGetValue(length, out var pair) == false)
        {
            pair = new();

            entries.Add(length, pair);
        }

        if(pair.freeEntries.Count > 0)
        {
            var entry = pair.freeEntries[0];

            pair.freeEntries.RemoveAt(0);

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
        
        if(entries.TryGetValue(contents.Length, out var pair) == false)
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

            pair.freeEntries.Add(entry);

            return;
        }
    }
}
