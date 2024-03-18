using System.Collections.Generic;

namespace Staple;

public class SkinnedMeshAnimator : IComponent
{
    public Mesh mesh;
    public string animation;
    public bool repeat = true;

    public SkinnedAnimationStateMachine stateMachine;
    public SkinnedAnimationController animationController;

    internal bool playInEditMode;
    internal float playTime;
    internal Dictionary<string, Transform> transformCache = new();
    internal Dictionary<string, MeshAsset.Node> nodeCache = new();
    internal SkinnedMeshAnimationEvaluator evaluator;

    public void SetAnimation(string name, bool repeat)
    {
        this.repeat = repeat;

        animation = name;
        playTime = 0;
        evaluator = null;
    }
}
