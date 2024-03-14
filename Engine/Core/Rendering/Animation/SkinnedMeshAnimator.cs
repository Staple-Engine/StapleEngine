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
    public bool repeat = true;

    public AnimationStateMachine stateMachine;

    internal bool playInEditMode;
    internal float playTime;
    internal Dictionary<string, Item> nodeRenderers = new();
    internal SkinnedMeshAnimationEvaluator evaluator;
    internal AnimationStateMachine.AnimationState currentState;
}
