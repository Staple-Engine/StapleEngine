using Staple.Utilities;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple.Internal;

internal class BufferAttributeContainer
{
    internal enum BufferSlot
    {
        Position,
        Normal,
        Tangent,
        Bitangent,
        BlendIndices,
        BlendWeights,
        Color0,
        Color1,
        Color2,
        Color3,
        TexCoord0,
        TexCoord1,
        TexCoord2,
        TexCoord3,
        TexCoord4,
        TexCoord5,
        TexCoord6,
        TexCoord7,
        Count,
    }

    public class Entries
    {
        public UnmanagedFreeformAllocator<Vector3>.Entry positionEntry;
        public UnmanagedFreeformAllocator<Vector3>.Entry normalEntry;
        public UnmanagedFreeformAllocator<Vector3>.Entry tangentEntry;
        public UnmanagedFreeformAllocator<Vector3>.Entry bitangentEntry;
        public UnmanagedFreeformAllocator<Vector4>.Entry blendIndicesEntry;
        public UnmanagedFreeformAllocator<Vector4>.Entry blendWeightsEntry;
        public UnmanagedFreeformAllocator<Color>.Entry color0Entry;
        public UnmanagedFreeformAllocator<Color>.Entry color1Entry;
        public UnmanagedFreeformAllocator<Color>.Entry color2Entry;
        public UnmanagedFreeformAllocator<Color>.Entry color3Entry;
        public UnmanagedFreeformAllocator<Vector2>.Entry texCoord0Entry;
        public UnmanagedFreeformAllocator<Vector2>.Entry texCoord1Entry;
        public UnmanagedFreeformAllocator<Vector2>.Entry texCoord2Entry;
        public UnmanagedFreeformAllocator<Vector2>.Entry texCoord3Entry;
        public UnmanagedFreeformAllocator<Vector2>.Entry texCoord4Entry;
        public UnmanagedFreeformAllocator<Vector2>.Entry texCoord5Entry;
        public UnmanagedFreeformAllocator<Vector2>.Entry texCoord6Entry;
        public UnmanagedFreeformAllocator<Vector2>.Entry texCoord7Entry;

        public UnmanagedFreeformAllocator<uint>.Entry indicesEntry;
    }

    public readonly BufferAttributeSource<Vector3, VertexBuffer> Position = new(VertexAttribute.Position, BufferSlot.Position);

    public readonly BufferAttributeSource<Vector3, VertexBuffer> Normal = new(VertexAttribute.Normal, BufferSlot.Normal, Vector3.Up);

    public readonly BufferAttributeSource<Vector3, VertexBuffer> Tangent = new(VertexAttribute.Tangent, BufferSlot.Tangent, Vector3.Up);

    public readonly BufferAttributeSource<Vector3, VertexBuffer> Bitangent = new(VertexAttribute.Bitangent, BufferSlot.Bitangent, Vector3.Up);

    public readonly BufferAttributeSource<Vector4, VertexBuffer> BlendIndices = new(VertexAttribute.BlendIndices, BufferSlot.BlendIndices);

    public readonly BufferAttributeSource<Vector4, VertexBuffer> BlendWeights = new(VertexAttribute.BlendWeights, BufferSlot.BlendWeights);

    public readonly BufferAttributeSource<Color, VertexBuffer> Color0 = new(VertexAttribute.Color0, BufferSlot.Color0, Color.White);

    public readonly BufferAttributeSource<Color, VertexBuffer> Color1 = new(VertexAttribute.Color1, BufferSlot.Color1, Color.White);

    public readonly BufferAttributeSource<Color, VertexBuffer> Color2 = new(VertexAttribute.Color2, BufferSlot.Color2, Color.White);

    public readonly BufferAttributeSource<Color, VertexBuffer> Color3 = new(VertexAttribute.Color3, BufferSlot.Color3, Color.White);

    public readonly BufferAttributeSource<Vector2, VertexBuffer> TexCoord0 = new(VertexAttribute.TexCoord0, BufferSlot.TexCoord0);

    public readonly BufferAttributeSource<Vector2, VertexBuffer> TexCoord1 = new(VertexAttribute.TexCoord1, BufferSlot.TexCoord1);

    public readonly BufferAttributeSource<Vector2, VertexBuffer> TexCoord2 = new(VertexAttribute.TexCoord2, BufferSlot.TexCoord2);

    public readonly BufferAttributeSource<Vector2, VertexBuffer> TexCoord3 = new(VertexAttribute.TexCoord3, BufferSlot.TexCoord3);

    public readonly BufferAttributeSource<Vector2, VertexBuffer> TexCoord4 = new(VertexAttribute.TexCoord4, BufferSlot.TexCoord4);

    public readonly BufferAttributeSource<Vector2, VertexBuffer> TexCoord5 = new(VertexAttribute.TexCoord5, BufferSlot.TexCoord5);

    public readonly BufferAttributeSource<Vector2, VertexBuffer> TexCoord6 = new(VertexAttribute.TexCoord6, BufferSlot.TexCoord6);

    public readonly BufferAttributeSource<Vector2, VertexBuffer> TexCoord7 = new(VertexAttribute.TexCoord7, BufferSlot.TexCoord7);

    public readonly BufferAttributeSource<uint, IndexBuffer> Indices = new(VertexAttribute.Position, BufferSlot.Count);

    private static void UpdateVertexBuffer<T>(BufferAttributeSource<T, VertexBuffer> buffer) where T: unmanaged
    {
        if (!buffer.Changed)
        {
            return;
        }

        buffer.Changed = false;

        RenderSystem.Backend.UpdateStaticMeshVertexBuffer(buffer);
    }

    private static void UpdateIndexBuffer(BufferAttributeSource<uint, IndexBuffer> buffer)
    {
        if (!buffer.Changed)
        {
            return;
        }

        buffer.Changed = false;

        RenderSystem.Backend.UpdateStaticMeshIndexBuffer(buffer);
    }

    public void Update()
    {
        UpdateVertexBuffer(Position);
        UpdateVertexBuffer(Normal);
        UpdateVertexBuffer(Tangent);
        UpdateVertexBuffer(Bitangent);
        UpdateVertexBuffer(BlendIndices);
        UpdateVertexBuffer(BlendWeights);
        UpdateVertexBuffer(Color0);
        UpdateVertexBuffer(Color1);
        UpdateVertexBuffer(Color2);
        UpdateVertexBuffer(Color3);
        UpdateVertexBuffer(TexCoord0);
        UpdateVertexBuffer(TexCoord1);
        UpdateVertexBuffer(TexCoord2);
        UpdateVertexBuffer(TexCoord3);
        UpdateVertexBuffer(TexCoord4);
        UpdateVertexBuffer(TexCoord5);
        UpdateVertexBuffer(TexCoord6);
        UpdateVertexBuffer(TexCoord7);

        UpdateIndexBuffer(Indices);
    }

    public Entries Allocate(int elements, int indices)
    {
        return new Entries()
        {
            positionEntry = Position.Allocate(elements),
            normalEntry = Normal.Allocate(elements),
            tangentEntry = Tangent.Allocate(elements),
            bitangentEntry = Bitangent.Allocate(elements),
            blendIndicesEntry = BlendIndices.Allocate(elements),
            blendWeightsEntry = BlendWeights.Allocate(elements),
            color0Entry = Color0.Allocate(elements),
            color1Entry = Color1.Allocate(elements),
            color2Entry = Color2.Allocate(elements),
            color3Entry = Color3.Allocate(elements),
            texCoord0Entry = TexCoord0.Allocate(elements),
            texCoord1Entry = TexCoord1.Allocate(elements),
            texCoord2Entry = TexCoord2.Allocate(elements),
            texCoord3Entry = TexCoord3.Allocate(elements),
            texCoord4Entry = TexCoord4.Allocate(elements),
            texCoord5Entry = TexCoord5.Allocate(elements),
            texCoord6Entry = TexCoord6.Allocate(elements),
            texCoord7Entry = TexCoord7.Allocate(elements),
            indicesEntry = Indices.Allocate(indices),
        };
    }

    public void Free(Entries entries)
    {
        Position.Free(entries.positionEntry);
        Normal.Free(entries.normalEntry);
        Tangent.Free(entries.tangentEntry);
        Bitangent.Free(entries.bitangentEntry);
        BlendIndices.Free(entries.blendIndicesEntry);
        BlendWeights.Free(entries.blendWeightsEntry);
        Color0.Free(entries.color0Entry);
        Color1.Free(entries.color1Entry);
        Color2.Free(entries.color2Entry);
        Color3.Free(entries.color3Entry);
        TexCoord0.Free(entries.texCoord0Entry);
        TexCoord1.Free(entries.texCoord1Entry);
        TexCoord2.Free(entries.texCoord2Entry);
        TexCoord3.Free(entries.texCoord3Entry);
        TexCoord4.Free(entries.texCoord4Entry);
        TexCoord5.Free(entries.texCoord5Entry);
        TexCoord6.Free(entries.texCoord6Entry);
        TexCoord7.Free(entries.texCoord7Entry);
    }

    public bool TryGetPositions(Entries entries, out Span<Vector3> positions, bool markChanged = false)
    {
        if(entries == null)
        {
            positions = default;

            return false;
        }

        if(markChanged)
        {
            Position.Changed = true;
        }

        positions = Position.allocator.Get(entries.positionEntry);

        return positions.Length > 0;
    }

    public bool TryGetNormals(Entries entries, out Span<Vector3> normals, bool markChanged = false)
    {
        if (entries == null)
        {
            normals = default;

            return false;
        }

        if (markChanged)
        {
            Normal.Changed = true;
        }

        normals = Normal.allocator.Get(entries.normalEntry);

        return normals.Length > 0;
    }

    public bool TryGetTangents(Entries entries, out Span<Vector3> tangents, bool markChanged = false)
    {
        if (entries == null)
        {
            tangents = default;

            return false;
        }

        if (markChanged)
        {
            Tangent.Changed = true;
        }

        tangents = Tangent.allocator.Get(entries.tangentEntry);

        return tangents.Length > 0;
    }

    public bool TryGetBitangents(Entries entries, out Span<Vector3> bitangents, bool markChanged = false)
    {
        if (entries == null)
        {
            bitangents = default;

            return false;
        }

        if (markChanged)
        {
            Bitangent.Changed = true;
        }

        bitangents = Bitangent.allocator.Get(entries.bitangentEntry);

        return bitangents.Length > 0;
    }

    public bool TryGetBlendIndices(Entries entries, out Span<Vector4> indices, bool markChanged = false)
    {
        if (entries == null)
        {
            indices = default;

            return false;
        }

        if (markChanged)
        {
            BlendIndices.Changed = true;
        }

        indices = BlendIndices.allocator.Get(entries.blendIndicesEntry);

        return indices.Length > 0;
    }

    public bool TryGetBlendWeights(Entries entries, out Span<Vector4> weights, bool markChanged = false)
    {
        if (entries == null)
        {
            weights = default;

            return false;
        }

        if (markChanged)
        {
            BlendWeights.Changed = true;
        }

        weights = BlendWeights.allocator.Get(entries.blendWeightsEntry);

        return weights.Length > 0;
    }

    public bool TryGetColor0(Entries entries, out Span<Color> colors, bool markChanged = false)
    {
        if (entries == null)
        {
            colors = default;

            return false;
        }

        if (markChanged)
        {
            Color0.Changed = true;
        }

        colors = Color0.allocator.Get(entries.color0Entry);

        return colors.Length > 0;
    }

    public bool TryGetColor1(Entries entries, out Span<Color> colors, bool markChanged = false)
    {
        if (entries == null)
        {
            colors = default;

            return false;
        }

        if (markChanged)
        {
            Color1.Changed = true;
        }

        colors = Color1.allocator.Get(entries.color1Entry);

        return colors.Length > 0;
    }

    public bool TryGetColor2(Entries entries, out Span<Color> colors, bool markChanged = false)
    {
        if (entries == null)
        {
            colors = default;

            return false;
        }

        if (markChanged)
        {
            Color2.Changed = true;
        }

        colors = Color2.allocator.Get(entries.color2Entry);

        return colors.Length > 0;
    }

    public bool TryGetColor3(Entries entries, out Span<Color> colors, bool markChanged = false)
    {
        if (entries == null)
        {
            colors = default;

            return false;
        }

        if (markChanged)
        {
            Color3.Changed = true;
        }

        colors = Color3.allocator.Get(entries.color3Entry);

        return colors.Length > 0;
    }

    public bool TryGetTexCoord0(Entries entries, out Span<Vector2> coords, bool markChanged = false)
    {
        if (entries == null)
        {
            coords = default;

            return false;
        }

        if (markChanged)
        {
            TexCoord0.Changed = true;
        }

        coords = TexCoord0.allocator.Get(entries.texCoord0Entry);

        return coords.Length > 0;
    }

    public bool TryGetTexCoord1(Entries entries, out Span<Vector2> coords, bool markChanged = false)
    {
        if (entries == null)
        {
            coords = default;

            return false;
        }

        if (markChanged)
        {
            TexCoord1.Changed = true;
        }

        coords = TexCoord1.allocator.Get(entries.texCoord1Entry);

        return coords.Length > 0;
    }

    public bool TryGetTexCoord2(Entries entries, out Span<Vector2> coords, bool markChanged = false)
    {
        if (entries == null)
        {
            coords = default;

            return false;
        }

        if (markChanged)
        {
            TexCoord2.Changed = true;
        }

        coords = TexCoord2.allocator.Get(entries.texCoord2Entry);

        return coords.Length > 0;
    }

    public bool TryGetTexCoord3(Entries entries, out Span<Vector2> coords, bool markChanged = false)
    {
        if (entries == null)
        {
            coords = default;

            return false;
        }

        if (markChanged)
        {
            TexCoord3.Changed = true;
        }

        coords = TexCoord3.allocator.Get(entries.texCoord3Entry);

        return coords.Length > 0;
    }

    public bool TryGetTexCoord4(Entries entries, out Span<Vector2> coords, bool markChanged = false)
    {
        if (entries == null)
        {
            coords = default;

            return false;
        }

        if (markChanged)
        {
            TexCoord4.Changed = true;
        }

        coords = TexCoord4.allocator.Get(entries.texCoord4Entry);

        return coords.Length > 0;
    }

    public bool TryGetTexCoord5(Entries entries, out Span<Vector2> coords, bool markChanged = false)
    {
        if (entries == null)
        {
            coords = default;

            return false;
        }

        if (markChanged)
        {
            TexCoord5.Changed = true;
        }

        coords = TexCoord5.allocator.Get(entries.texCoord5Entry);

        return coords.Length > 0;
    }

    public bool TryGetTexCoord6(Entries entries, out Span<Vector2> coords, bool markChanged = false)
    {
        if (entries == null)
        {
            coords = default;

            return false;
        }

        if (markChanged)
        {
            TexCoord6.Changed = true;
        }

        coords = TexCoord6.allocator.Get(entries.texCoord6Entry);

        return coords.Length > 0;
    }

    public bool TryGetTexCoord7(Entries entries, out Span<Vector2> coords, bool markChanged = false)
    {
        if (entries == null)
        {
            coords = default;

            return false;
        }

        if (markChanged)
        {
            TexCoord7.Changed = true;
        }

        coords = TexCoord7.allocator.Get(entries.texCoord7Entry);

        return coords.Length > 0;
    }

    public bool TryGetIndices(Entries entries, out Span<uint> indices, bool markChanged = false)
    {
        if (entries == null)
        {
            indices = default;

            return false;
        }

        if (markChanged)
        {
            Indices.Changed = true;
        }

        indices = Indices.allocator.Get(entries.indicesEntry);

        return indices.Length > 0;
    }

    public static int BufferIndex(VertexAttribute attribute)
    {
        var slot = attribute switch
        {
            VertexAttribute.Position => BufferSlot.Position,
            VertexAttribute.Normal => BufferSlot.Normal,
            VertexAttribute.Tangent => BufferSlot.Tangent,
            VertexAttribute.Bitangent => BufferSlot.Bitangent,
            VertexAttribute.Color0 => BufferSlot.Color0,
            VertexAttribute.Color1 => BufferSlot.Color1,
            VertexAttribute.Color2 => BufferSlot.Color2,
            VertexAttribute.Color3 => BufferSlot.Color3,
            VertexAttribute.BlendIndices => BufferSlot.BlendIndices,
            VertexAttribute.BlendWeights => BufferSlot.BlendWeights,
            VertexAttribute.TexCoord0 => BufferSlot.TexCoord0,
            VertexAttribute.TexCoord1 => BufferSlot.TexCoord1,
            VertexAttribute.TexCoord2 => BufferSlot.TexCoord2,
            VertexAttribute.TexCoord3 => BufferSlot.TexCoord3,
            VertexAttribute.TexCoord4 => BufferSlot.TexCoord4,
            VertexAttribute.TexCoord5 => BufferSlot.TexCoord5,
            VertexAttribute.TexCoord6 => BufferSlot.TexCoord6,
            VertexAttribute.TexCoord7 => BufferSlot.TexCoord7,
            _ => BufferSlot.Count,
        };

        return (int)slot;
    }

    public static int BufferElementSize(VertexAttribute attribute)
    {
        return attribute switch
        {
            VertexAttribute.Position or
                VertexAttribute.Normal or
                VertexAttribute.Tangent or
                VertexAttribute.Bitangent => Marshal.SizeOf<Vector3>(),
            VertexAttribute.Color0 or
                VertexAttribute.Color1 or
                VertexAttribute.Color2 or
                VertexAttribute.Color3 or
                VertexAttribute.BlendIndices or
                VertexAttribute.BlendWeights => Marshal.SizeOf<Vector4>(),
            VertexAttribute.TexCoord0 or
                VertexAttribute.TexCoord1 or
                VertexAttribute.TexCoord2 or
                VertexAttribute.TexCoord3 or
                VertexAttribute.TexCoord4 or
                VertexAttribute.TexCoord5 or
                VertexAttribute.TexCoord6 or
                VertexAttribute.TexCoord7 => Marshal.SizeOf<Vector2>(),
            _ => 0,
        };
    }

    public static int BufferElementSize(int bufferIndex)
    {
        var slot = (BufferSlot)bufferIndex;
        
        return slot switch
        {
            BufferSlot.Position or BufferSlot.Normal or BufferSlot.Tangent or BufferSlot.Bitangent => Marshal.SizeOf<Vector3>(),
            BufferSlot.Color0 or BufferSlot.Color1 or BufferSlot.Color2 or BufferSlot.Color3 or
                BufferSlot.BlendIndices or BufferSlot.BlendWeights => Marshal.SizeOf<Vector4>(),
            BufferSlot.TexCoord0 or BufferSlot.TexCoord1 or BufferSlot.TexCoord2 or
                BufferSlot.TexCoord3 or BufferSlot.TexCoord4 or BufferSlot.TexCoord5 or
                BufferSlot.TexCoord6 or BufferSlot.TexCoord7 => Marshal.SizeOf<Vector2>(),
            _ => 0,
        };
    }
}
