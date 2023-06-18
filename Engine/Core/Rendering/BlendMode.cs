using Bgfx;

namespace Staple
{
    /// <summary>
    /// Blending mode for shaders
    /// </summary>
    public enum BlendMode : ulong
    {
        Off,
        Zero = bgfx.StateFlags.BlendZero,
        One = bgfx.StateFlags.BlendOne,
        SrcColor = bgfx.StateFlags.BlendSrcColor,
        OneMinusSrcColor = bgfx.StateFlags.BlendInvSrcColor,
        SrcAlpha = bgfx.StateFlags.BlendSrcAlpha,
        OneMinusSrcAlpha = bgfx.StateFlags.BlendInvSrcAlpha,
        DstAlpha = bgfx.StateFlags.BlendDstAlpha,
        OneMinusDstAlpha = bgfx.StateFlags.BlendInvDstAlpha,
        DstColor = bgfx.StateFlags.BlendDstColor,
        OneMinusDstColor = bgfx.StateFlags.BlendInvDstColor,
        SrcAlphaSat = bgfx.StateFlags.BlendSrcAlphaSat,
    }
}
