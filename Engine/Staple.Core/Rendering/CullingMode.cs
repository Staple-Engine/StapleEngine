namespace Staple;

/// <summary>
/// What kind of culling to use, to optimize rendering
/// </summary>
public enum CullingMode
{
    /// <summary>
    /// No culling
    /// </summary>
    None,

    /// <summary>
    /// Cull back facing faces
    /// </summary>
    Back,

    /// <summary>
    /// Cull front facing faces
    /// </summary>
    Front,
}
