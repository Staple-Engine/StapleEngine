using System.Collections.Generic;

namespace Staple;

public class SkinnedMeshAnimator : IComponent
{
    internal class Item
    {
        public MeshAsset.Node node;
        public Transform transform;
    }

    public Mesh mesh;
    public string animation;

    internal Dictionary<string, Item> nodeRenderers = new();
    internal SkinnedMeshAnimationEvaluator evaluator;
}
