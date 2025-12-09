using SDL3;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Staple.Internal;

internal class SDLGPUVertexLayoutBuilder : VertexLayoutBuilder
{
    private readonly List<SDL.GPUVertexAttribute> attributes = [];
    private readonly List<VertexAttribute> vertexAttributes = [];
    private int offset = 0;
    private SDLGPUVertexLayout layout;

    public override VertexLayoutBuilder Add(VertexAttribute name, VertexAttributeType type)
    {
        if(completed)
        {
            return this;
        }

        base.Add(name, type);

        vertexAttributes.Add(name);

        attributes.Add(new SDL.GPUVertexAttribute()
        {
            Format = type switch
            {
                VertexAttributeType.Byte2 => SDL.GPUVertexElementFormat.Byte2,
                VertexAttributeType.Byte4 => SDL.GPUVertexElementFormat.Byte4,

                VertexAttributeType.UByte2 => SDL.GPUVertexElementFormat.Ubyte2,
                VertexAttributeType.UByte4 => SDL.GPUVertexElementFormat.Ubyte4,

                VertexAttributeType.Byte2Norm => SDL.GPUVertexElementFormat.Byte2Norm,
                VertexAttributeType.Byte4Norm => SDL.GPUVertexElementFormat.Byte4Norm,

                VertexAttributeType.UByte2Norm => SDL.GPUVertexElementFormat.Ubyte2Norm,
                VertexAttributeType.UByte4Norm => SDL.GPUVertexElementFormat.Ubyte4Norm,

                VertexAttributeType.Float => SDL.GPUVertexElementFormat.Float,
                VertexAttributeType.Float2 => SDL.GPUVertexElementFormat.Float2,
                VertexAttributeType.Float3 => SDL.GPUVertexElementFormat.Float3,
                VertexAttributeType.Float4 => SDL.GPUVertexElementFormat.Float4,

                VertexAttributeType.Half2 => SDL.GPUVertexElementFormat.Half2,
                VertexAttributeType.Half4 => SDL.GPUVertexElementFormat.Half4,

                VertexAttributeType.Int => SDL.GPUVertexElementFormat.Int,
                VertexAttributeType.Int2 => SDL.GPUVertexElementFormat.Int2,
                VertexAttributeType.Int3 => SDL.GPUVertexElementFormat.Int3,
                VertexAttributeType.Int4 => SDL.GPUVertexElementFormat.Int4,

                VertexAttributeType.UInt => SDL.GPUVertexElementFormat.Uint,
                VertexAttributeType.UInt2 => SDL.GPUVertexElementFormat.Uint2,
                VertexAttributeType.UInt3 => SDL.GPUVertexElementFormat.Uint3,
                VertexAttributeType.UInt4 => SDL.GPUVertexElementFormat.Uint4,

                VertexAttributeType.Short2 => SDL.GPUVertexElementFormat.Short2,
                VertexAttributeType.Short4 => SDL.GPUVertexElementFormat.Short4,

                VertexAttributeType.UShort2 => SDL.GPUVertexElementFormat.Ushort2,
                VertexAttributeType.UShort4 => SDL.GPUVertexElementFormat.Ushort4,

                VertexAttributeType.Short2Norm => SDL.GPUVertexElementFormat.Short2Norm,
                VertexAttributeType.Short4Norm => SDL.GPUVertexElementFormat.Short4Norm,

                VertexAttributeType.UShort2Norm => SDL.GPUVertexElementFormat.Ushort2Norm,
                VertexAttributeType.UShort4Norm => SDL.GPUVertexElementFormat.Ushort4Norm,

                _ => throw new System.ArgumentOutOfRangeException(nameof(type), "Not a valid data type"),
            },
            Offset = (uint)offset,
        });

        offset += type switch
        {
            VertexAttributeType.Byte2 => 2,
            VertexAttributeType.Byte4 => 4,

            VertexAttributeType.UByte2 => 2,
            VertexAttributeType.UByte4 => 4,

            VertexAttributeType.Byte2Norm => 2,
            VertexAttributeType.Byte4Norm => 4,

            VertexAttributeType.UByte2Norm => 2,
            VertexAttributeType.UByte4Norm => 4,

            VertexAttributeType.Float => sizeof(float),
            VertexAttributeType.Float2 => sizeof(float) * 2,
            VertexAttributeType.Float3 => sizeof(float) * 3,
            VertexAttributeType.Float4 => sizeof(float) * 4,

            VertexAttributeType.Half2 => sizeof(ushort) * 2,
            VertexAttributeType.Half4 => sizeof(ushort) * 4,

            VertexAttributeType.Int => sizeof(int),
            VertexAttributeType.Int2 => sizeof(int) * 2,
            VertexAttributeType.Int3 => sizeof(int) * 3,
            VertexAttributeType.Int4 => sizeof(int) * 4,

            VertexAttributeType.UInt => sizeof(int),
            VertexAttributeType.UInt2 => sizeof(int) * 2,
            VertexAttributeType.UInt3 => sizeof(int) * 3,
            VertexAttributeType.UInt4 => sizeof(int) * 4,

            VertexAttributeType.Short2 => sizeof(short) * 2,
            VertexAttributeType.Short4 => sizeof(short) * 4,

            VertexAttributeType.UShort2 => sizeof(short) * 2,
            VertexAttributeType.UShort4 => sizeof(short) * 4,

            VertexAttributeType.Short2Norm => sizeof(short) * 2,
            VertexAttributeType.Short4Norm => sizeof(short) * 4,

            VertexAttributeType.UShort2Norm => sizeof(short) * 2,
            VertexAttributeType.UShort4Norm => sizeof(short) * 4,
            _ => 0,
        };

        return this;
    }

    public override VertexLayout Build()
    {
        if(attributes.Count == 0)
        {
            return null;
        }

        if(completed)
        {
            return layout;
        }

        completed = true;

        layout = new SDLGPUVertexLayout(CollectionsMarshal.AsSpan(attributes), vertexAttributes, components, offset);

        return layout;
    }
}
