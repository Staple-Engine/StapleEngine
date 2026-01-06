using Staple.Utilities;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple.Internal;

internal class BufferAttributeContainer
{
    internal const int PositionBufferSlot = 0;
    internal const int NormalBufferSlot = 1;
    internal const int TangentBufferSlot = 2;
    internal const int BitangentBufferSlot = 3;
    internal const int BlendIndicesBufferSlot = 4;
    internal const int BlendWeightsBufferSlot = 5;
    internal const int Color0BufferSlot = 6;
    internal const int Color1BufferSlot = 7;
    internal const int Color2BufferSlot = 8;
    internal const int Color3BufferSlot = 9;
    internal const int TexCoord0BufferSlot = 10;
    internal const int TexCoord1BufferSlot = 11;
    internal const int TexCoord2BufferSlot = 12;
    internal const int TexCoord3BufferSlot = 13;
    internal const int TexCoord4BufferSlot = 14;
    internal const int TexCoord5BufferSlot = 15;
    internal const int TexCoord6BufferSlot = 16;
    internal const int TexCoord7BufferSlot = 17;

    public class Entries
    {
        public FreeformAllocator<Vector3>.Entry positionEntry;
        public FreeformAllocator<Vector3>.Entry normalEntry;
        public FreeformAllocator<Vector3>.Entry tangentEntry;
        public FreeformAllocator<Vector3>.Entry bitangentEntry;
        public FreeformAllocator<Vector4>.Entry blendIndicesEntry;
        public FreeformAllocator<Vector4>.Entry blendWeightsEntry;
        public FreeformAllocator<Color>.Entry color0Entry;
        public FreeformAllocator<Color>.Entry color1Entry;
        public FreeformAllocator<Color>.Entry color2Entry;
        public FreeformAllocator<Color>.Entry color3Entry;
        public FreeformAllocator<Vector2>.Entry texCoord0Entry;
        public FreeformAllocator<Vector2>.Entry texCoord1Entry;
        public FreeformAllocator<Vector2>.Entry texCoord2Entry;
        public FreeformAllocator<Vector2>.Entry texCoord3Entry;
        public FreeformAllocator<Vector2>.Entry texCoord4Entry;
        public FreeformAllocator<Vector2>.Entry texCoord5Entry;
        public FreeformAllocator<Vector2>.Entry texCoord6Entry;
        public FreeformAllocator<Vector2>.Entry texCoord7Entry;

        public FreeformAllocator<uint>.Entry indicesEntry;
    }

    public readonly BufferAttributeSource<Vector3, VertexBuffer> Position = new(VertexAttribute.Position, PositionBufferSlot);

    public readonly BufferAttributeSource<Vector3, VertexBuffer> Normal = new(VertexAttribute.Normal, NormalBufferSlot, Vector3.Up);

    public readonly BufferAttributeSource<Vector3, VertexBuffer> Tangent = new(VertexAttribute.Tangent, TangentBufferSlot, Vector3.Up);

    public readonly BufferAttributeSource<Vector3, VertexBuffer> Bitangent = new(VertexAttribute.Bitangent, BitangentBufferSlot, Vector3.Up);

    public readonly BufferAttributeSource<Vector4, VertexBuffer> BlendIndices = new(VertexAttribute.BlendIndices, BlendIndicesBufferSlot);

    public readonly BufferAttributeSource<Vector4, VertexBuffer> BlendWeights = new(VertexAttribute.BlendWeights, BlendWeightsBufferSlot);

    public readonly BufferAttributeSource<Color, VertexBuffer> Color0 = new(VertexAttribute.Color0, Color0BufferSlot, Color.White);

    public readonly BufferAttributeSource<Color, VertexBuffer> Color1 = new(VertexAttribute.Color1, Color1BufferSlot, Color.White);

    public readonly BufferAttributeSource<Color, VertexBuffer> Color2 = new(VertexAttribute.Color2, Color2BufferSlot, Color.White);

    public readonly BufferAttributeSource<Color, VertexBuffer> Color3 = new(VertexAttribute.Color3, Color3BufferSlot, Color.White);

    public readonly BufferAttributeSource<Vector2, VertexBuffer> TexCoord0 = new(VertexAttribute.TexCoord0, TexCoord0BufferSlot);

    public readonly BufferAttributeSource<Vector2, VertexBuffer> TexCoord1 = new(VertexAttribute.TexCoord1, TexCoord1BufferSlot);

    public readonly BufferAttributeSource<Vector2, VertexBuffer> TexCoord2 = new(VertexAttribute.TexCoord2, TexCoord2BufferSlot);

    public readonly BufferAttributeSource<Vector2, VertexBuffer> TexCoord3 = new(VertexAttribute.TexCoord3, TexCoord3BufferSlot);

    public readonly BufferAttributeSource<Vector2, VertexBuffer> TexCoord4 = new(VertexAttribute.TexCoord4, TexCoord4BufferSlot);

    public readonly BufferAttributeSource<Vector2, VertexBuffer> TexCoord5 = new(VertexAttribute.TexCoord5, TexCoord5BufferSlot);

    public readonly BufferAttributeSource<Vector2, VertexBuffer> TexCoord6 = new(VertexAttribute.TexCoord6, TexCoord6BufferSlot);

    public readonly BufferAttributeSource<Vector2, VertexBuffer> TexCoord7 = new(VertexAttribute.TexCoord7, TexCoord7BufferSlot);

    public readonly BufferAttributeSource<uint, IndexBuffer> Indices = new(VertexAttribute.Position, -1);

    private static void UpdateVertexBuffer<T>(BufferAttributeSource<T, VertexBuffer> buffer) where T: unmanaged
    {
        if (buffer.Changed == false)
        {
            return;
        }

        buffer.Changed = false;

        RenderSystem.Backend.UpdateStaticMeshVertexBuffer(buffer);
    }

    private static void UpdateIndexBuffer(BufferAttributeSource<uint, IndexBuffer> buffer)
    {
        if (buffer.Changed == false)
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
        return attribute switch
        {
            VertexAttribute.Position => PositionBufferSlot,
            VertexAttribute.Normal => NormalBufferSlot,
            VertexAttribute.Tangent => TangentBufferSlot,
            VertexAttribute.Bitangent => BitangentBufferSlot,
            VertexAttribute.Color0 => Color0BufferSlot,
            VertexAttribute.Color1 => Color1BufferSlot,
            VertexAttribute.Color2 => Color2BufferSlot,
            VertexAttribute.Color3 => Color3BufferSlot,
            VertexAttribute.BlendIndices => BlendIndicesBufferSlot,
            VertexAttribute.BlendWeights => BlendWeightsBufferSlot,
            VertexAttribute.TexCoord0 => TexCoord0BufferSlot,
            VertexAttribute.TexCoord1 => TexCoord1BufferSlot,
            VertexAttribute.TexCoord2 => TexCoord2BufferSlot,
            VertexAttribute.TexCoord3 => TexCoord3BufferSlot,
            VertexAttribute.TexCoord4 => TexCoord4BufferSlot,
            VertexAttribute.TexCoord5 => TexCoord5BufferSlot,
            VertexAttribute.TexCoord6 => TexCoord6BufferSlot,
            VertexAttribute.TexCoord7 => TexCoord7BufferSlot,
            _ => -1,
        };
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
        return bufferIndex switch
        {
            PositionBufferSlot or NormalBufferSlot or TangentBufferSlot or BitangentBufferSlot => Marshal.SizeOf<Vector3>(),
            Color0BufferSlot or Color1BufferSlot or Color2BufferSlot or Color3BufferSlot or
                BlendIndicesBufferSlot or BlendWeightsBufferSlot => Marshal.SizeOf<Vector4>(),
            TexCoord0BufferSlot or TexCoord1BufferSlot or TexCoord2BufferSlot or
                TexCoord3BufferSlot or TexCoord4BufferSlot or TexCoord5BufferSlot or
                TexCoord6BufferSlot or TexCoord7BufferSlot => Marshal.SizeOf<Vector2>(),
            _ => 0,
        };
    }
}
