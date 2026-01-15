
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

internal class ManagedFreeformAllocator<T>
{
    public class Entry
    {
        public int start;
        public int length;
        public bool freed = false;
    }

    internal readonly List<Entry> freeEntries = [];

    internal readonly int elementSize = Marshal.SizeOf<T>();

    private readonly List<Entry> entries = [];

    public T[] buffer = [];

    public void Compact(int extraLength = 0)
    {
        var compactedLength = buffer.Length;

        foreach (var entry in freeEntries)
        {
            compactedLength -= entry.length;
        }

        var newBuffer = new T[compactedLength + extraLength];

        var newPosition = 0;

        foreach (var entry in entries)
        {
            Array.Copy(buffer, entry.start, newBuffer, newPosition, entry.length);

            newPosition += entry.length;
        }

        newPosition = 0;

        foreach (var entry in entries)
        {
            entry.start = newPosition;

            newPosition += entry.length;
        }

        freeEntries.Clear();

        buffer = newBuffer;
    }

    public Entry Allocate(int length)
    {
        Entry outValue = null;

        if (freeEntries.Count == 0)
        {
            var start = buffer.Length;

            Array.Resize(ref buffer, buffer.Length + length);

            outValue = new Entry()
            {
                start = start,
                length = length,
            };

            entries.Add(outValue);

            return outValue;
        }

        for (var i = 0; i < freeEntries.Count; i++)
        {
            var entry = freeEntries[i];

            if (length <= entry.length)
            {
                freeEntries.RemoveAt(i);

                var difference = entry.length - length;

                if (difference > 0)
                {
                    freeEntries.Add(new()
                    {
                        start = entry.start + length,
                        length = difference,
                        freed = true,
                    });
                }

                outValue = new Entry()
                {
                    start = entry.start,
                    length = length,
                };

                entries.Add(outValue);

                entries.Sort((a, b) => a.start.CompareTo(b.start));

                return outValue;
            }
        }

        Compact(length);

        outValue = new Entry()
        {
            start = buffer.Length - length,
            length = length,
        };

        entries.Add(outValue);

        return outValue;
    }

    public void Free(Entry entry)
    {
        if (entry.freed)
        {
            return;
        }

        entry.freed = true;

        entries.Remove(entry);

        freeEntries.Add(entry);
    }

    public Span<T> Get(Entry entry)
    {
        if (entry.freed ||
            entry.start >= buffer.Length ||
            entry.start + entry.length > buffer.Length)
        {
            return default;
        }

        return buffer.AsSpan(entry.start, entry.length);
    }
}
