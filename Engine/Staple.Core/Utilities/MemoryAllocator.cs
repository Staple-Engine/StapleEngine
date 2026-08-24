using System;
using System.Runtime.InteropServices;

namespace Staple.Utilities;

public class MemoryAllocator
{
    public readonly ExpandableContainer<byte> buffer = new(1024);

    internal int position;

    public Span<byte> Allocate(int size)
    {
        var targetSize = position + size;

        if (targetSize >= buffer.Length)
        {
            var newSize = buffer.Length * 2;

            while (newSize < targetSize)
            {
                newSize *= 2;
            }

            newSize *= 2;

            buffer.Resize(newSize, true);
        }

        var outValue = buffer.RawContents.AsSpan(position, size);

        position += size;

        return outValue;
    }

    public void Clear()
    {
        position = 0;
    }

    public Span<byte> GetSpan(int position, int size)
    {
        return buffer.RawContents.AsSpan(position, size);
    }
}

public class MemoryAllocator<T> where T: unmanaged
{
    public readonly ExpandableContainer<T> buffer = new(1024);

    private readonly int elementSize = Marshal.SizeOf<T>();

    internal int position;

    public Span<T> Allocate(int size)
    {
        var targetSize = position + size;

        if (targetSize >= buffer.Length)
        {
            var newSize = buffer.Length * 2;

            while (newSize < targetSize)
            {
                newSize *= 2;
            }

            newSize *= 2;

            buffer.Resize(newSize, true);
        }

        var outValue = buffer.RawContents.AsSpan(position, size);

        position += size;

        return outValue;
    }

    public void Clear()
    {
        position = 0;
    }

    public Span<T> GetSpan(int position, int size)
    {
        return buffer.RawContents.AsSpan(position, size);
    }
}
