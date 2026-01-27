using SDL;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Staple.Internal;

internal class SDLGPUVertexLayoutBuilder : VertexLayoutBuilder
{
    private readonly List<SDL_GPUVertexAttribute> attributes = [];
    private readonly List<VertexAttribute> vertexAttributes = [];
    private int offset;
    private SDLGPUVertexLayout layout;

    public override VertexLayoutBuilder Add(VertexAttribute name, VertexAttributeType type)
    {
        if(completed)
        {
            return this;
        }

        base.Add(name, type);

        vertexAttributes.Add(name);

        attributes.Add(new SDL_GPUVertexAttribute()
        {
            format = type switch
            {
                VertexAttributeType.Byte2 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_BYTE2,
                VertexAttributeType.Byte4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_BYTE4,

                VertexAttributeType.UByte2 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE2,
                VertexAttributeType.UByte4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4,

                VertexAttributeType.Byte2Norm => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_BYTE2_NORM,
                VertexAttributeType.Byte4Norm => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_BYTE4_NORM,

                VertexAttributeType.UByte2Norm => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE2_NORM,
                VertexAttributeType.UByte4Norm => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4_NORM,

                VertexAttributeType.Float => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT,
                VertexAttributeType.Float2 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
                VertexAttributeType.Float3 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
                VertexAttributeType.Float4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4,

                VertexAttributeType.Half2 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_HALF2,
                VertexAttributeType.Half4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_HALF4,

                VertexAttributeType.Int => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_INT,
                VertexAttributeType.Int2 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_INT2,
                VertexAttributeType.Int3 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_INT3,
                VertexAttributeType.Int4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_INT4,

                VertexAttributeType.UInt => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UINT,
                VertexAttributeType.UInt2 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UINT2,
                VertexAttributeType.UInt3 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UINT3,
                VertexAttributeType.UInt4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UINT4,

                VertexAttributeType.Short2 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_SHORT2,
                VertexAttributeType.Short4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_SHORT4,

                VertexAttributeType.UShort2 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_USHORT2,
                VertexAttributeType.UShort4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_USHORT4,

                VertexAttributeType.Short2Norm => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_SHORT2_NORM,
                VertexAttributeType.Short4Norm => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_SHORT4_NORM,

                VertexAttributeType.UShort2Norm => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_USHORT2_NORM,
                VertexAttributeType.UShort4Norm => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_USHORT4_NORM,

                _ => throw new System.ArgumentOutOfRangeException(nameof(type), "Not a valid data type"),
            },
            offset = (uint)offset,
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
