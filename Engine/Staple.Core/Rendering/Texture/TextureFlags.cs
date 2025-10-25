using System;
using System.Text.Json.Serialization;

namespace Staple;

[JsonConverter(typeof(JsonStringEnumConverter<TextureFlags>))]
[Flags]
public enum TextureFlags
{
    None = 0,
    SRGB = (1 << 0),
    RepeatU = (1 << 1),
    MirrorU = (1 << 2),
    ClampU = (1 << 3),
    RepeatV = (1 << 4),
    MirrorV = (1 << 5),
    ClampV = (1 << 6),
    RepeatW = (1 << 7),
    MirrorW = (1 << 8),
    ClampW = (1 << 9),
    PointFilter = (1 << 10),
    LinearFilter = (1 << 11),
    AnisotropicFilter = (1 << 12),
    ColorTarget = (1 << 13),
    DepthStencilTarget = (1 << 14),
    Readback = (1 << 15),
    ComputeRead = (1 << 16),
    ComputeWrite = (1 << 17),
    TextureType2DArray = (1 << 18),
    TextureType3D = (1 << 19),
    TextureTypeCube = (1 << 20),
    TextureTypeCubeArray = (1 << 21),
}
