namespace Staple.Internal;

/// <summary>
/// Describes the current state of verifying culling for a renderable
/// </summary>
internal enum CullingState
{
    /// <summary>
    /// No validation has been done
    /// </summary>
    None,
    /// <summary>
    /// Object is visible
    /// </summary>
    Visible,
    /// <summary>
    /// Object is invisible
    /// </summary>
    Invisible,
}
