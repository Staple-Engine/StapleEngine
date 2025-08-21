using System.Numerics;

namespace Staple;

/// <summary>
/// Represents a volume that will be checked for culling (prevent rendering of its contents if needed).
/// This is useful for simplifying culling tests among a group of renderables
/// </summary>
public sealed class CullingVolume : IComponent
{
    /// <summary>
    /// The type of culling to perform
    /// </summary>
    public enum CullingType
    {
        /// <summary>
        /// Check the bounds of child renderers
        /// </summary>
        Renderers,
        /// <summary>
        /// Use specified bounds
        /// </summary>
        Bounds,
    }

    /// <summary>
    /// The type of culling
    /// </summary>
    public CullingType type = CullingType.Renderers;

    /// <summary>
    /// The bounds if <see cref="CullingType.Bounds"/> is selected.
    /// Think of this as the size of a box around this volume's position.
    /// </summary>
    public Vector3 bounds = new(1, 1, 1);

    /// <summary>
    /// The renderers this entity contains
    /// </summary>
    internal EntityQuery<Transform, Renderable> renderers;

    /// <summary>
    /// The volumes this entity contains
    /// </summary>
    internal EntityQuery<Transform, CullingVolume> children;

    /// <summary>
    /// Cached bounds coordinates to reduce allocations.
    /// </summary>
    internal Vector3[] boundsCoordinates = [];

    /// <summary>
    /// Whether this culling volume needs to be updated
    /// </summary>
    internal bool needsUpdate;
}
