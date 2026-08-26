using System.Numerics;

namespace Staple;

/// <summary>
/// Contains info on a raycast hit result in 3D Physics
/// </summary>
/// <param name="body">The body that was hit</param>
/// <param name="fraction">The fraction of the hit</param>
/// <param name="position">The world position of the hit</param>
public readonly struct RaycastHit3D(IBody3D body, float fraction, Vector3 position)
{
    /// <summary>
    /// The body that was hit
    /// </summary>
    public readonly IBody3D body = body;

    /// <summary>
    /// The fraction of the hit
    /// </summary>
    public readonly float fraction = fraction;

    /// <summary>
    /// The world position of the hit
    /// </summary>
    public readonly Vector3 position = position;
}
