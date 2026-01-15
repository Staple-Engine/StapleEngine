using Staple.Utilities;
using System;
using System.Numerics;

namespace Staple.Internal;

internal class BufferAttributeSource<T, BufferType>(VertexAttribute attribute, BufferAttributeContainer.BufferSlot slot, T? defaultValue = null) where T: unmanaged
{
    public readonly UnmanagedFreeformAllocator<T> allocator = new();

    public readonly VertexAttribute attribute = attribute;

    public readonly VertexAttributeType attributeType = typeof(T) switch
    {
        Type t when t == typeof(Vector2) => VertexAttributeType.Float2,
        Type t when t == typeof(Vector3) => VertexAttributeType.Float3,
        Type t when t == typeof(Vector4) => VertexAttributeType.Float4,
        Type t when t == typeof(Color) => VertexAttributeType.Float4,
        Type t when t == typeof(uint) => VertexAttributeType.UInt,
        Type t when t == typeof(int) => VertexAttributeType.Int,
        _ => throw new NotSupportedException($"Data type for buffer attribute not supported: {typeof(T).FullName}"),
    };

    public readonly int index = (int)slot;

    public bool Changed { get; set; }

    public UnmanagedFreeformAllocator<T>.Entry Allocate(int elementCount)
    {
        Changed = true;

        var entry = allocator.Allocate(elementCount);

        if (!defaultValue.HasValue)
        {
            return entry;
        }
        
        var value = defaultValue.Value;

        var span = allocator.Get(entry);

        for (var i = 0; i < span.Length; i++)
        {
            span[i] = value;
        }

        return entry;
    }

    public void Free(UnmanagedFreeformAllocator<T>.Entry entry)
    {
        Changed = true;

        allocator.Free(entry);
    }
}
