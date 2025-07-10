using System.Numerics;

namespace Staple;

public sealed class CullingVolume : IComponent
{
    internal EntityQuery<Transform, Renderable> renderers;

    internal Vector3[] boundsCoordinates = [];
}
