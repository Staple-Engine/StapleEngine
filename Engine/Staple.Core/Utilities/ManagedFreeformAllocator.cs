
using Staple;
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

    private ExpandableContainer<T> buffer = new();
    private ExpandableContainer<T> stagingBuffer = new();

    public int Length => buffer.Length;

    public Span<T> Contents => buffer.Contents;

    public void Compact(int extraLength = 0)
    {
        var compactedLength = buffer.Length;

        foreach (var entry in freeEntries)
        {
            compactedLength -= entry.length;
        }

        stagingBuffer.Clear();

        foreach (var entry in entries)
        {
            stagingBuffer.AddRange(buffer.Contents.Slice(entry.start, entry.length));
        }

        var newPosition = 0;

        foreach (var entry in entries)
        {
            entry.start = newPosition;

            newPosition += entry.length;
        }

        freeEntries.Clear();

        stagingBuffer.Resize(compactedLength + extraLength, true);

        (buffer, stagingBuffer) = (stagingBuffer, buffer);
    }

    public Entry Allocate(int length)
    {
        Entry outValue = null;

        if (freeEntries.Count == 0)
        {
            var start = buffer.Length;

            buffer.Resize(buffer.Length + length, true);

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

        return buffer.Contents.Slice(entry.start, entry.length);
    }
}
