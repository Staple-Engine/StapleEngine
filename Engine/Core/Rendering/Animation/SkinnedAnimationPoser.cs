using System.Collections.Generic;

namespace Staple;

public class SkinnedAnimationPoser : IComponent
{
    public Mesh mesh;

    internal Dictionary<string, MeshAsset.Node> nodeCache = new();
    internal Dictionary<string, Transform> transformCache = new();
}
