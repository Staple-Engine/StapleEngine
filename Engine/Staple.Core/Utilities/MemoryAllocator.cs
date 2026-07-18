using System;
using System.Runtime.InteropServices;

namespace Staple.Utilities;

public class MemoryAllocator
{
    public readonly ExpandableContainer<byte> buffer = new(1024);

    private GCHandle pinHandle;

    private nint pinAddress;

    private bool needsRepin;

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

            needsRepin = true;
        }

        var outValue = buffer.Contents.Slice(position, size);

        position += size;

        return outValue;
    }

    private void Repin()
    {
        if (pinHandle.IsAllocated)
        {
            pinHandle.Free();
        }

        pinHandle = GCHandle.Alloc(buffer.RawContents, GCHandleType.Pinned);
        pinAddress = pinHandle.AddrOfPinnedObject();
    }

    public void EnsurePin()
    {
        if(needsRepin)
        {
            needsRepin = false;

            Repin();

            return;
        }

        if (pinHandle.IsAllocated)
        {
            return;
        }

        pinHandle = GCHandle.Alloc(buffer.RawContents, GCHandleType.Pinned);
        pinAddress = pinHandle.AddrOfPinnedObject();
    }

    public void Clear()
    {
        position = 0;
    }

    public Span<byte> GetSpan(int position, int size)
    {
        return buffer.Contents.Slice(position, size);
    }

    public nint Get(int position)
    {
        EnsurePin();

        return pinAddress + position;
    }
}

public class MemoryAllocator<T> where T: unmanaged
{
    public readonly ExpandableContainer<T> buffer = new(1024);

    private GCHandle pinHandle;

    private nint pinAddress;

    private readonly int elementSize = Marshal.SizeOf<T>();

    private bool needsRepin;

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

            needsRepin = true;
        }

        var outValue = buffer.Contents.Slice(position, size);

        position += size;

        return outValue;
    }

    private void Repin()
    {
        if (pinHandle.IsAllocated)
        {
            pinHandle.Free();
        }

        pinHandle = GCHandle.Alloc(buffer.RawContents, GCHandleType.Pinned);
        pinAddress = pinHandle.AddrOfPinnedObject();
    }

    public void EnsurePin()
    {
        if(needsRepin)
        {
            needsRepin = false;

            Repin();

            return;
        }

        if (pinHandle.IsAllocated)
        {
            return;
        }

        pinHandle = GCHandle.Alloc(buffer.RawContents, GCHandleType.Pinned);
        pinAddress = pinHandle.AddrOfPinnedObject();
    }

    public void Clear()
    {
        position = 0;
    }

    public Span<T> GetSpan(int position, int size)
    {
        return buffer.Contents.Slice(position, size);
    }

    public nint Get(int position)
    {
        EnsurePin();

        return pinAddress + position * elementSize;
    }
}
