namespace Staple;

public enum TextureFormat
{
    /// <summary>
    /// DXT1 R5G6B5A1
    /// </summary>
    BC1,

    /// <summary>
    /// DXT3 R5G6B5A4
    /// </summary>
    BC2,

    /// <summary>
    /// DXT5 R5G6B5A8
    /// </summary>
    BC3,

    /// <summary>
    /// LATC1/ATI1 R8
    /// </summary>
    BC4,

    /// <summary>
    /// LATC2/ATI2 RG8
    /// </summary>
    BC5,

    /// <summary>
    /// BC6H RGB16F
    /// </summary>
    BC6H,

    /// <summary>
    /// BC7 RGB 4-7 bits per color channel, 0-8 bits alpha
    /// </summary>
    BC7,

    /// <summary>
    /// ETC1 RGB8
    /// </summary>
    ETC1,

    /// <summary>
    /// ETC2 RGB8
    /// </summary>
    ETC2,

    /// <summary>
    /// ETC2 RGBA8
    /// </summary>
    ETC2A,

    /// <summary>
    /// ETC2 RGB8A1
    /// </summary>
    ETC2A1,

    /// <summary>
    /// PVRTC1 RGB 2BPP
    /// </summary>
    PTC12,

    /// <summary>
    /// PVRTC1 RGB 4BPP
    /// </summary>
    PTC14,

    /// <summary>
    /// PVRTC1 RGBA 2BPP
    /// </summary>
    PTC12A,

    /// <summary>
    /// PVRTC1 RGBA 4BPP
    /// </summary>
    PTC14A,

    /// <summary>
    /// PVRTC2 RGBA 2BPP
    /// </summary>
    PTC22,

    /// <summary>
    /// PVRTC2 RGBA 4BPP
    /// </summary>
    PTC24,

    /// <summary>
    /// ATC RGB 4BPP
    /// </summary>
    ATC,

    /// <summary>
    /// ATCE RGBA 8 BPP explicit alpha
    /// </summary>
    ATCE,

    /// <summary>
    /// ATCI RGBA 8 BPP interpolated alpha
    /// </summary>
    ATCI,

    /// <summary>
    /// ASTC 4x4 8.0 BPP
    /// </summary>
    ASTC4x4,

    /// <summary>
    /// ASTC 5x4 6.40 BPP
    /// </summary>
    ASTC5x4,

    /// <summary>
    /// ASTC 5x5 5.12 BPP
    /// </summary>
    ASTC5x5,

    /// <summary>
    /// ASTC 6x5 4.27 BPP
    /// </summary>
    ASTC6x5,

    /// <summary>
    /// ASTC 6x6 3.56 BPP
    /// </summary>
    ASTC6x6,

    /// <summary>
    /// ASTC 8x5 3.20 BPP
    /// </summary>
    ASTC8x5,

    /// <summary>
    /// ASTC 8x6 2.67 BPP
    /// </summary>
    ASTC8x6,

    /// <summary>
    /// ASTC 8x8 2.00 BPP
    /// </summary>
    ASTC8x8,

    /// <summary>
    /// ASTC 10x5 2.56 BPP
    /// </summary>
    ASTC10x5,

    /// <summary>
    /// ASTC 10x6 2.13 BPP
    /// </summary>
    ASTC10x6,

    /// <summary>
    /// ASTC 10x8 1.60 BPP
    /// </summary>
    ASTC10x8,

    /// <summary>
    /// ASTC 10x10 1.28 BPP
    /// </summary>
    ASTC10x10,

    /// <summary>
    /// ASTC 12x10 1.07 BPP
    /// </summary>
    ASTC12x10,

    /// <summary>
    /// ASTC 12x12 0.89 BPP
    /// </summary>
    ASTC12x12,

    /// <summary>
    /// Compressed formats above.
    /// </summary>
    Unknown,
    R1,
    A8,
    R8,
    R8I,
    R8U,
    R8S,
    R16,
    R16I,
    R16U,
    R16F,
    R16S,
    R32I,
    R32U,
    R32F,
    RG8,
    RG8I,
    RG8U,
    RG8S,
    RG16,
    RG16I,
    RG16U,
    RG16F,
    RG16S,
    RG32I,
    RG32U,
    RG32F,
    RGB8,
    RGB8I,
    RGB8U,
    RGB8S,
    RGB9E5F,
    BGRA8,
    RGBA8,
    RGBA8I,
    RGBA8U,
    RGBA8S,
    RGBA16,
    RGBA16I,
    RGBA16U,
    RGBA16F,
    RGBA16S,
    RGBA32I,
    RGBA32U,
    RGBA32F,
    B5G6R5,
    R5G6B5,
    BGRA4,
    RGBA4,
    BGR5A1,
    RGB5A1,
    RGB10A2,
    RG11B10F,

    /// <summary>
    /// Depth formats below.
    /// </summary>
    UnknownDepth,
    D16,
    D24,
    D24S8,
    D32,
    D16F,
    D24F,
    D32F,
    D0S8,
}
