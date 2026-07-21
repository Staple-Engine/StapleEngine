namespace Staple;

/// <summary>
/// Render queue for sorting materials
/// </summary>
public enum MaterialRenderQueue
{
    /// <summary>
    /// Generally opaque geometry
    /// </summary>
    Opaque = 0,

    /// <summary>
    /// Alpha test/cutout geometry
    /// </summary>
    AlphaTest = 1000,

    /// <summary>
    /// Transparent geometry
    /// </summary>
    Transparent = 2000,

    /// <summary>
    /// Geometry that goes on top of everything else
    /// </summary>
    Overlay = 3000,
}
