namespace Staple;

/// <summary>
/// Blending mode for shaders
/// </summary>
public enum BlendMode : ulong
{
    Off,
    Zero,
    One,
    SrcColor,
    OneMinusSrcColor,
    SrcAlpha,
    OneMinusSrcAlpha,
    DstAlpha,
    OneMinusDstAlpha,
    DstColor,
    OneMinusDstColor,
    SrcAlphaSat,
}
