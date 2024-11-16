using Bgfx;

namespace Staple.Internal;

internal static class BGFXUtils
{
    public static bgfx.TextureFormat GetTextureFormat(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.BC1 => bgfx.TextureFormat.BC1,
            TextureFormat.BC2 => bgfx.TextureFormat.BC2,
            TextureFormat.BC3 => bgfx.TextureFormat.BC3,
            TextureFormat.BC4 => bgfx.TextureFormat.BC4,
            TextureFormat.BC5 => bgfx.TextureFormat.BC5,
            TextureFormat.BC6H => bgfx.TextureFormat.BC6H,
            TextureFormat.BC7 => bgfx.TextureFormat.BC7,
            TextureFormat.ETC1 => bgfx.TextureFormat.ETC1,
            TextureFormat.ETC2 => bgfx.TextureFormat.ETC2,
            TextureFormat.ETC2A => bgfx.TextureFormat.ETC2A,
            TextureFormat.ETC2A1 => bgfx.TextureFormat.ETC2A1,
            TextureFormat.PTC12 => bgfx.TextureFormat.PTC12,
            TextureFormat.PTC14 => bgfx.TextureFormat.PTC14,
            TextureFormat.PTC12A => bgfx.TextureFormat.PTC12A,
            TextureFormat.PTC14A => bgfx.TextureFormat.PTC14A,
            TextureFormat.PTC22 => bgfx.TextureFormat.PTC22,
            TextureFormat.PTC24 => bgfx.TextureFormat.PTC24,
            TextureFormat.ATC => bgfx.TextureFormat.ATC,
            TextureFormat.ATCE => bgfx.TextureFormat.ATCE,
            TextureFormat.ATCI => bgfx.TextureFormat.ATCI,
            TextureFormat.ASTC4x4 => bgfx.TextureFormat.ASTC4x4,
            TextureFormat.ASTC5x4 => bgfx.TextureFormat.ASTC5x4,
            TextureFormat.ASTC5x5 => bgfx.TextureFormat.ASTC5x5,
            TextureFormat.ASTC6x5 => bgfx.TextureFormat.ASTC6x5,
            TextureFormat.ASTC6x6 => bgfx.TextureFormat.ASTC6x6,
            TextureFormat.ASTC8x5 => bgfx.TextureFormat.ASTC8x5,
            TextureFormat.ASTC8x6 => bgfx.TextureFormat.ASTC8x6,
            TextureFormat.ASTC8x8 => bgfx.TextureFormat.ASTC8x8,
            TextureFormat.ASTC10x5 => bgfx.TextureFormat.ASTC10x5,
            TextureFormat.ASTC10x6 => bgfx.TextureFormat.ASTC10x6,
            TextureFormat.ASTC10x8 => bgfx.TextureFormat.ASTC10x8,
            TextureFormat.ASTC10x10 => bgfx.TextureFormat.ASTC10x10,
            TextureFormat.ASTC12x10 => bgfx.TextureFormat.ASTC12x10,
            TextureFormat.ASTC12x12 => bgfx.TextureFormat.ASTC12x12,
            TextureFormat.Unknown => bgfx.TextureFormat.Unknown,
            TextureFormat.R1 => bgfx.TextureFormat.R1,
            TextureFormat.A8 => bgfx.TextureFormat.A8,
            TextureFormat.R8 => bgfx.TextureFormat.R8,
            TextureFormat.R8I => bgfx.TextureFormat.R8I,
            TextureFormat.R8U => bgfx.TextureFormat.R8U,
            TextureFormat.R8S => bgfx.TextureFormat.R8S,
            TextureFormat.R16 => bgfx.TextureFormat.R16,
            TextureFormat.R16I => bgfx.TextureFormat.R16I,
            TextureFormat.R16U => bgfx.TextureFormat.R16U,
            TextureFormat.R16F => bgfx.TextureFormat.R16F,
            TextureFormat.R16S => bgfx.TextureFormat.R16S,
            TextureFormat.R32I => bgfx.TextureFormat.R32I,
            TextureFormat.R32U => bgfx.TextureFormat.R32U,
            TextureFormat.R32F => bgfx.TextureFormat.R32F,
            TextureFormat.RG8 => bgfx.TextureFormat.RG8,
            TextureFormat.RG8I => bgfx.TextureFormat.RG8I,
            TextureFormat.RG8U => bgfx.TextureFormat.RG8U,
            TextureFormat.RG8S => bgfx.TextureFormat.RG8S,
            TextureFormat.RG16 => bgfx.TextureFormat.RG16,
            TextureFormat.RG16I => bgfx.TextureFormat.RG16I,
            TextureFormat.RG16U => bgfx.TextureFormat.RG16U,
            TextureFormat.RG16F => bgfx.TextureFormat.RG16F,
            TextureFormat.RG16S => bgfx.TextureFormat.RG16S,
            TextureFormat.RG32I => bgfx.TextureFormat.RG32I,
            TextureFormat.RG32U => bgfx.TextureFormat.RG32U,
            TextureFormat.RG32F => bgfx.TextureFormat.RG32F,
            TextureFormat.RGB8 => bgfx.TextureFormat.RGB8,
            TextureFormat.RGB8I => bgfx.TextureFormat.RGB8I,
            TextureFormat.RGB8U => bgfx.TextureFormat.RGB8U,
            TextureFormat.RGB8S => bgfx.TextureFormat.RGB8S,
            TextureFormat.RGB9E5F => bgfx.TextureFormat.RGB9E5F,
            TextureFormat.BGRA8 => bgfx.TextureFormat.BGRA8,
            TextureFormat.RGBA8 => bgfx.TextureFormat.RGBA8,
            TextureFormat.RGBA8I => bgfx.TextureFormat.RGBA8I,
            TextureFormat.RGBA8U => bgfx.TextureFormat.RGBA8U,
            TextureFormat.RGBA8S => bgfx.TextureFormat.RGBA8S,
            TextureFormat.RGBA16 => bgfx.TextureFormat.RGBA16,
            TextureFormat.RGBA16I => bgfx.TextureFormat.RGBA16I,
            TextureFormat.RGBA16U => bgfx.TextureFormat.RGBA16U,
            TextureFormat.RGBA16F => bgfx.TextureFormat.RGBA16F,
            TextureFormat.RGBA16S => bgfx.TextureFormat.RGBA16S,
            TextureFormat.RGBA32I => bgfx.TextureFormat.RGBA32I,
            TextureFormat.RGBA32U => bgfx.TextureFormat.RGBA32U,
            TextureFormat.RGBA32F => bgfx.TextureFormat.RGBA32F,
            TextureFormat.B5G6R5 => bgfx.TextureFormat.B5G6R5,
            TextureFormat.R5G6B5 => bgfx.TextureFormat.R5G6B5,
            TextureFormat.BGRA4 => bgfx.TextureFormat.BGRA4,
            TextureFormat.RGBA4 => bgfx.TextureFormat.RGBA4,
            TextureFormat.BGR5A1 => bgfx.TextureFormat.BGR5A1,
            TextureFormat.RGB5A1 => bgfx.TextureFormat.RGB5A1,
            TextureFormat.RGB10A2 => bgfx.TextureFormat.RGB10A2,
            TextureFormat.RG11B10F => bgfx.TextureFormat.RG11B10F,
            TextureFormat.UnknownDepth => bgfx.TextureFormat.UnknownDepth,
            TextureFormat.D16 => bgfx.TextureFormat.D16,
            TextureFormat.D24 => bgfx.TextureFormat.D24,
            TextureFormat.D24S8 => bgfx.TextureFormat.D24S8,
            TextureFormat.D32 => bgfx.TextureFormat.D32,
            TextureFormat.D16F => bgfx.TextureFormat.D16F,
            TextureFormat.D24F => bgfx.TextureFormat.D24F,
            TextureFormat.D32F => bgfx.TextureFormat.D32F,
            TextureFormat.D0S8 => bgfx.TextureFormat.D0S8,
            _ => bgfx.TextureFormat.Unknown,
        };
    }

    public static TextureFormat GetBGFXTextureFormat(bgfx.TextureFormat format)
    {
        return format switch
        {
           bgfx.TextureFormat.BC1 => TextureFormat.BC1,
           bgfx.TextureFormat.BC2 => TextureFormat.BC2,
           bgfx.TextureFormat.BC3 => TextureFormat.BC3,
           bgfx.TextureFormat.BC4 => TextureFormat.BC4,
           bgfx.TextureFormat.BC5 => TextureFormat.BC5,
           bgfx.TextureFormat.BC6H => TextureFormat.BC6H,
           bgfx.TextureFormat.BC7 => TextureFormat.BC7,
           bgfx.TextureFormat.ETC1 => TextureFormat.ETC1,
           bgfx.TextureFormat.ETC2 => TextureFormat.ETC2,
           bgfx.TextureFormat.ETC2A => TextureFormat.ETC2A,
           bgfx.TextureFormat.ETC2A1 => TextureFormat.ETC2A1,
           bgfx.TextureFormat.PTC12 => TextureFormat.PTC12,
           bgfx.TextureFormat.PTC14 => TextureFormat.PTC14,
           bgfx.TextureFormat.PTC12A => TextureFormat.PTC12A,
           bgfx.TextureFormat.PTC14A => TextureFormat.PTC14A,
           bgfx.TextureFormat.PTC22 => TextureFormat.PTC22,
           bgfx.TextureFormat.PTC24 => TextureFormat.PTC24,
           bgfx.TextureFormat.ATC => TextureFormat.ATC,
           bgfx.TextureFormat.ATCE => TextureFormat.ATCE,
           bgfx.TextureFormat.ATCI => TextureFormat.ATCI,
           bgfx.TextureFormat.ASTC4x4 => TextureFormat.ASTC4x4,
           bgfx.TextureFormat.ASTC5x4 => TextureFormat.ASTC5x4,
           bgfx.TextureFormat.ASTC5x5 => TextureFormat.ASTC5x5,
           bgfx.TextureFormat.ASTC6x5 => TextureFormat.ASTC6x5,
           bgfx.TextureFormat.ASTC6x6 => TextureFormat.ASTC6x6,
           bgfx.TextureFormat.ASTC8x5 => TextureFormat.ASTC8x5,
           bgfx.TextureFormat.ASTC8x6 => TextureFormat.ASTC8x6,
           bgfx.TextureFormat.ASTC8x8 => TextureFormat.ASTC8x8,
           bgfx.TextureFormat.ASTC10x5 => TextureFormat.ASTC10x5,
           bgfx.TextureFormat.ASTC10x6 => TextureFormat.ASTC10x6,
           bgfx.TextureFormat.ASTC10x8 => TextureFormat.ASTC10x8,
           bgfx.TextureFormat.ASTC10x10 => TextureFormat.ASTC10x10,
           bgfx.TextureFormat.ASTC12x10 => TextureFormat.ASTC12x10,
           bgfx.TextureFormat.ASTC12x12 => TextureFormat.ASTC12x12,
           bgfx.TextureFormat.Unknown => TextureFormat.Unknown,
           bgfx.TextureFormat.R1 => TextureFormat.R1,
           bgfx.TextureFormat.A8 => TextureFormat.A8,
           bgfx.TextureFormat.R8 => TextureFormat.R8,
           bgfx.TextureFormat.R8I => TextureFormat.R8I,
           bgfx.TextureFormat.R8U => TextureFormat.R8U,
           bgfx.TextureFormat.R8S => TextureFormat.R8S,
           bgfx.TextureFormat.R16 => TextureFormat.R16,
           bgfx.TextureFormat.R16I => TextureFormat.R16I,
           bgfx.TextureFormat.R16U => TextureFormat.R16U,
           bgfx.TextureFormat.R16F => TextureFormat.R16F,
           bgfx.TextureFormat.R16S => TextureFormat.R16S,
           bgfx.TextureFormat.R32I => TextureFormat.R32I,
           bgfx.TextureFormat.R32U => TextureFormat.R32U,
           bgfx.TextureFormat.R32F => TextureFormat.R32F,
           bgfx.TextureFormat.RG8 => TextureFormat.RG8,
           bgfx.TextureFormat.RG8I => TextureFormat.RG8I,
           bgfx.TextureFormat.RG8U => TextureFormat.RG8U,
           bgfx.TextureFormat.RG8S => TextureFormat.RG8S,
           bgfx.TextureFormat.RG16 => TextureFormat.RG16,
           bgfx.TextureFormat.RG16I => TextureFormat.RG16I,
           bgfx.TextureFormat.RG16U => TextureFormat.RG16U,
           bgfx.TextureFormat.RG16F => TextureFormat.RG16F,
           bgfx.TextureFormat.RG16S => TextureFormat.RG16S,
           bgfx.TextureFormat.RG32I => TextureFormat.RG32I,
           bgfx.TextureFormat.RG32U => TextureFormat.RG32U,
           bgfx.TextureFormat.RG32F => TextureFormat.RG32F,
           bgfx.TextureFormat.RGB8 => TextureFormat.RGB8,
           bgfx.TextureFormat.RGB8I => TextureFormat.RGB8I,
           bgfx.TextureFormat.RGB8U => TextureFormat.RGB8U,
           bgfx.TextureFormat.RGB8S => TextureFormat.RGB8S,
           bgfx.TextureFormat.RGB9E5F => TextureFormat.RGB9E5F,
           bgfx.TextureFormat.BGRA8 => TextureFormat.BGRA8,
           bgfx.TextureFormat.RGBA8 => TextureFormat.RGBA8,
           bgfx.TextureFormat.RGBA8I => TextureFormat.RGBA8I,
           bgfx.TextureFormat.RGBA8U => TextureFormat.RGBA8U,
           bgfx.TextureFormat.RGBA8S => TextureFormat.RGBA8S,
           bgfx.TextureFormat.RGBA16 => TextureFormat.RGBA16,
           bgfx.TextureFormat.RGBA16I => TextureFormat.RGBA16I,
           bgfx.TextureFormat.RGBA16U => TextureFormat.RGBA16U,
           bgfx.TextureFormat.RGBA16F => TextureFormat.RGBA16F,
           bgfx.TextureFormat.RGBA16S => TextureFormat.RGBA16S,
           bgfx.TextureFormat.RGBA32I => TextureFormat.RGBA32I,
           bgfx.TextureFormat.RGBA32U => TextureFormat.RGBA32U,
           bgfx.TextureFormat.RGBA32F => TextureFormat.RGBA32F,
           bgfx.TextureFormat.B5G6R5 => TextureFormat.B5G6R5,
           bgfx.TextureFormat.R5G6B5 => TextureFormat.R5G6B5,
           bgfx.TextureFormat.BGRA4 => TextureFormat.BGRA4,
           bgfx.TextureFormat.RGBA4 => TextureFormat.RGBA4,
           bgfx.TextureFormat.BGR5A1 => TextureFormat.BGR5A1,
           bgfx.TextureFormat.RGB5A1 => TextureFormat.RGB5A1,
           bgfx.TextureFormat.RGB10A2 => TextureFormat.RGB10A2,
           bgfx.TextureFormat.RG11B10F => TextureFormat.RG11B10F,
           bgfx.TextureFormat.UnknownDepth => TextureFormat.UnknownDepth,
           bgfx.TextureFormat.D16 => TextureFormat.D16,
           bgfx.TextureFormat.D24 => TextureFormat.D24,
           bgfx.TextureFormat.D24S8 => TextureFormat.D24S8,
           bgfx.TextureFormat.D32 => TextureFormat.D32,
           bgfx.TextureFormat.D16F => TextureFormat.D16F,
           bgfx.TextureFormat.D24F => TextureFormat.D24F,
           bgfx.TextureFormat.D32F => TextureFormat.D32F,
           bgfx.TextureFormat.D0S8 => TextureFormat.D0S8,
            _ => TextureFormat.Unknown,
        };
    }

    public static bgfx.BackbufferRatio GetBackbufferRatio(RenderTargetBackbufferRatio ratio)
    {
        return ratio switch
        {
            RenderTargetBackbufferRatio.Equal => bgfx.BackbufferRatio.Equal,
            RenderTargetBackbufferRatio.Half => bgfx.BackbufferRatio.Half,
            RenderTargetBackbufferRatio.Quarter => bgfx.BackbufferRatio.Quarter,
            RenderTargetBackbufferRatio.Eighth => bgfx.BackbufferRatio.Eighth,
            RenderTargetBackbufferRatio.Sixteenth => bgfx.BackbufferRatio.Sixteenth,
            RenderTargetBackbufferRatio.Double => bgfx.BackbufferRatio.Double,
            _ => bgfx.BackbufferRatio.Equal,
        };
    }

    public static VertexAttribute GetVertexAttribute(bgfx.Attrib attribute)
    {
        return attribute switch
        {
            bgfx.Attrib.Bitangent => VertexAttribute.Bitangent,
            bgfx.Attrib.Color0 => VertexAttribute.Color0,
            bgfx.Attrib.Color1 => VertexAttribute.Color1,
            bgfx.Attrib.Color2 => VertexAttribute.Color2,
            bgfx.Attrib.Color3 => VertexAttribute.Color3,
            bgfx.Attrib.Indices => VertexAttribute.BoneIndices,
            bgfx.Attrib.Normal => VertexAttribute.Normal,
            bgfx.Attrib.Position => VertexAttribute.Position,
            bgfx.Attrib.Tangent => VertexAttribute.Tangent,
            bgfx.Attrib.TexCoord0 => VertexAttribute.TexCoord0,
            bgfx.Attrib.TexCoord1 => VertexAttribute.TexCoord1,
            bgfx.Attrib.TexCoord2 => VertexAttribute.TexCoord2,
            bgfx.Attrib.TexCoord3 => VertexAttribute.TexCoord3,
            bgfx.Attrib.TexCoord4 => VertexAttribute.TexCoord4,
            bgfx.Attrib.TexCoord5 => VertexAttribute.TexCoord5,
            bgfx.Attrib.TexCoord6 => VertexAttribute.TexCoord6,
            bgfx.Attrib.TexCoord7 => VertexAttribute.TexCoord7,
            bgfx.Attrib.Weight => VertexAttribute.BoneWeight,
            _ => VertexAttribute.Position,
        };
    }

    public static bgfx.Attrib GetBGFXVertexAttribute(VertexAttribute attribute)
    {
        return attribute switch
        {
            VertexAttribute.Bitangent => bgfx.Attrib.Bitangent,
            VertexAttribute.Color0 => bgfx.Attrib.Color0,
            VertexAttribute.Color1 => bgfx.Attrib.Color1,
            VertexAttribute.Color2 => bgfx.Attrib.Color2,
            VertexAttribute.Color3 => bgfx.Attrib.Color3,
            VertexAttribute.BoneIndices => bgfx.Attrib.Indices,
            VertexAttribute.Normal => bgfx.Attrib.Normal,
            VertexAttribute.Position => bgfx.Attrib.Position,
            VertexAttribute.Tangent => bgfx.Attrib.Tangent,
            VertexAttribute.TexCoord0 => bgfx.Attrib.TexCoord0,
            VertexAttribute.TexCoord1 => bgfx.Attrib.TexCoord1,
            VertexAttribute.TexCoord2 => bgfx.Attrib.TexCoord2,
            VertexAttribute.TexCoord3 => bgfx.Attrib.TexCoord3,
            VertexAttribute.TexCoord4 => bgfx.Attrib.TexCoord4,
            VertexAttribute.TexCoord5 => bgfx.Attrib.TexCoord5,
            VertexAttribute.TexCoord6 => bgfx.Attrib.TexCoord6,
            VertexAttribute.TexCoord7 => bgfx.Attrib.TexCoord7,
            VertexAttribute.BoneWeight => bgfx.Attrib.Weight,
            _ => bgfx.Attrib.Position,
        };
    }

    public static VertexAttributeType GetVertexAttributeType(bgfx.AttribType type)
    {
        return type switch
        {
            bgfx.AttribType.Float => VertexAttributeType.Float,
            bgfx.AttribType.Half => VertexAttributeType.Half,
            bgfx.AttribType.Int16 => VertexAttributeType.Int16,
            bgfx.AttribType.Uint10 => VertexAttributeType.Uint10,
            bgfx.AttribType.Uint8 => VertexAttributeType.Uint8,
            _ => VertexAttributeType.Float,
        };
    }

    public static bgfx.AttribType GetBGFXVertexAttributeType(VertexAttributeType type)
    {
        return type switch
        {
            VertexAttributeType.Float => bgfx.AttribType.Float,
            VertexAttributeType.Half => bgfx.AttribType.Half,
            VertexAttributeType.Int16 => bgfx.AttribType.Int16,
            VertexAttributeType.Uint10 => bgfx.AttribType.Uint10,
            VertexAttributeType.Uint8 => bgfx.AttribType.Uint8,
            _ => bgfx.AttribType.Float,
        };
    }

    public static RenderBufferFlags GetRenderBufferFlags(bgfx.BufferFlags flags)
    {
        var outValue = RenderBufferFlags.None;

        if (flags.HasFlag(bgfx.BufferFlags.Index32))
        {
            outValue |= RenderBufferFlags.Index32;
        }

        if (flags.HasFlag(bgfx.BufferFlags.AllowResize))
        {
            outValue |= RenderBufferFlags.AllowResize;
        }

        if (flags.HasFlag(bgfx.BufferFlags.ComputeFormat8x1))
        {
            outValue |= RenderBufferFlags.Compute8x1;
        }

        if (flags.HasFlag(bgfx.BufferFlags.ComputeFormat8x2))
        {
            outValue |= RenderBufferFlags.Compute8x2;
        }

        if (flags.HasFlag(bgfx.BufferFlags.ComputeFormat8x4))
        {
            outValue |= RenderBufferFlags.Compute8x4;
        }

        if (flags.HasFlag(bgfx.BufferFlags.ComputeFormat16x1))
        {
            outValue |= RenderBufferFlags.Compute16x1;
        }

        if (flags.HasFlag(bgfx.BufferFlags.ComputeFormat16x2))
        {
            outValue |= RenderBufferFlags.Compute16x2;
        }

        if (flags.HasFlag(bgfx.BufferFlags.ComputeFormat16x4))
        {
            outValue |= RenderBufferFlags.Compute16x4;
        }

        if (flags.HasFlag(bgfx.BufferFlags.ComputeFormat32x1))
        {
            outValue |= RenderBufferFlags.Compute32x1;
        }

        if (flags.HasFlag(bgfx.BufferFlags.ComputeFormat32x2))
        {
            outValue |= RenderBufferFlags.Compute32x2;
        }

        if (flags.HasFlag(bgfx.BufferFlags.ComputeFormat32x4))
        {
            outValue |= RenderBufferFlags.Compute32x4;
        }

        if (flags.HasFlag(bgfx.BufferFlags.ComputeTypeInt))
        {
            outValue |= RenderBufferFlags.ComputeTypeInt;
        }

        if (flags.HasFlag(bgfx.BufferFlags.ComputeTypeUint))
        {
            outValue |= RenderBufferFlags.ComputeTypeUInt;
        }

        if (flags.HasFlag(bgfx.BufferFlags.ComputeTypeFloat))
        {
            outValue |= RenderBufferFlags.ComputeTypeFloat;
        }

        if (flags.HasFlag(bgfx.BufferFlags.ComputeWrite))
        {
            outValue |= RenderBufferFlags.ComputeWrite;
        }

        if (flags.HasFlag(bgfx.BufferFlags.ComputeRead))
        {
            outValue |= RenderBufferFlags.ComputeRead;
        }

        if (flags.HasFlag(bgfx.BufferFlags.ComputeReadWrite))
        {
            outValue |= RenderBufferFlags.ComputeReadWrite;
        }

        if (flags.HasFlag(bgfx.BufferFlags.DrawIndirect))
        {
            outValue |= RenderBufferFlags.DrawIndirect;
        }

        return outValue;
    }

    public static bgfx.BufferFlags GetBGFXBufferFlags(RenderBufferFlags flags)
    {
        var outValue = bgfx.BufferFlags.None;

        if (flags.HasFlag(RenderBufferFlags.Index32))
        {
            outValue |= bgfx.BufferFlags.Index32;
        }

        if (flags.HasFlag(RenderBufferFlags.AllowResize))
        {
            outValue |= bgfx.BufferFlags.AllowResize;
        }

        if (flags.HasFlag(RenderBufferFlags.Compute8x1))
        {
            outValue |= bgfx.BufferFlags.ComputeFormat8x1;
        }

        if (flags.HasFlag(RenderBufferFlags.Compute8x2))
        {
            outValue |= bgfx.BufferFlags.ComputeFormat8x2;
        }

        if (flags.HasFlag(RenderBufferFlags.Compute8x4))
        {
            outValue |= bgfx.BufferFlags.ComputeFormat8x4;
        }

        if (flags.HasFlag(RenderBufferFlags.Compute16x1))
        {
            outValue |= bgfx.BufferFlags.ComputeFormat16x1;
        }

        if (flags.HasFlag(RenderBufferFlags.Compute16x2))
        {
            outValue |= bgfx.BufferFlags.ComputeFormat16x2;
        }

        if (flags.HasFlag(RenderBufferFlags.Compute16x4))
        {
            outValue |= bgfx.BufferFlags.ComputeFormat16x4;
        }

        if (flags.HasFlag(RenderBufferFlags.Compute32x1))
        {
            outValue |= bgfx.BufferFlags.ComputeFormat32x1;
        }

        if (flags.HasFlag(RenderBufferFlags.Compute32x2))
        {
            outValue |= bgfx.BufferFlags.ComputeFormat32x2;
        }

        if (flags.HasFlag(RenderBufferFlags.Compute32x4))
        {
            outValue |= bgfx.BufferFlags.ComputeFormat32x4;
        }

        if (flags.HasFlag(RenderBufferFlags.ComputeTypeInt))
        {
            outValue |= bgfx.BufferFlags.ComputeTypeInt;
        }

        if (flags.HasFlag(RenderBufferFlags.ComputeTypeUInt))
        {
            outValue |= bgfx.BufferFlags.ComputeTypeUint;
        }

        if (flags.HasFlag(RenderBufferFlags.ComputeTypeFloat))
        {
            outValue |= bgfx.BufferFlags.ComputeTypeFloat;
        }

        if (flags.HasFlag(RenderBufferFlags.ComputeWrite))
        {
            outValue |= bgfx.BufferFlags.ComputeWrite;
        }

        if (flags.HasFlag(RenderBufferFlags.ComputeRead))
        {
            outValue |= bgfx.BufferFlags.ComputeRead;
        }

        if (flags.HasFlag(RenderBufferFlags.ComputeReadWrite))
        {
            outValue |= bgfx.BufferFlags.ComputeReadWrite;
        }

        if (flags.HasFlag(RenderBufferFlags.DrawIndirect))
        {
            outValue |= bgfx.BufferFlags.DrawIndirect;
        }

        return outValue;
    }

    public static Access GetAccess(bgfx.Access access)
    {
        return access switch
        {
            bgfx.Access.Read => Access.Read,
            bgfx.Access.Write => Access.Write,
            bgfx.Access.ReadWrite => Access.ReadWrite,
            _ => Access.Read,
        };
    }

    public static bgfx.Access GetBGFXAccess(Access access)
    {
        return access switch
        {
            Access.Read => bgfx.Access.Read,
            Access.Write => bgfx.Access.Write,
            Access.ReadWrite => bgfx.Access.ReadWrite,
            _ => bgfx.Access.Read,
        };
    }
}
