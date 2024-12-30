using System.Collections.Generic;
using System.Numerics;

namespace Staple.Internal;

/// <summary>
/// Calculates the animations for a skinned mesh
/// </summary>
internal class SkinnedMeshAnimationEvaluator
{
    /// <summary>
    /// Whether we've finished playing
    /// </summary>
    public bool FinishedPlaying
    {
        get
        {
            return animator.repeat == false &&
                animator.playTime * animation.ticksPerSecond >= animation.duration;
        }
    }

    /// <summary>
    /// The animation to animate
    /// </summary>
    internal readonly MeshAsset.Animation animation;

    /// <summary>
    /// The mesh asset
    /// </summary>
    private readonly MeshAsset meshAsset;

    /// <summary>
    /// The animator component
    /// </summary>
    private readonly SkinnedMeshAnimator animator;

    /// <summary>
    /// The nodes for animating
    /// </summary>
    internal MeshAsset.Node[] nodes;

    /// <summary>
    /// Cache of last indices for positions (channel index -> frame index)
    /// </summary>
    private readonly Dictionary<int, int> lastPositionIndex = [];

    /// <summary>
    /// Cache of last indices for rotations (channel index -> frame index)
    /// </summary>
    private readonly Dictionary<int, int> lastRotationIndex = [];

    /// <summary>
    /// Cache of last indices for scales (channel index -> frame index)
    /// </summary>
    private readonly Dictionary<int, int> lastScaleIndex = [];

    /// <summary>
    /// Last update time
    /// </summary>
    private float lastTime;

    /// <summary>
    /// Update timer to limit evaluations
    /// </summary>
    private float updateTimer;

    /// <summary>
    /// Time to wait between frames before evaluating
    /// </summary>
    private readonly float timeBetweenFrames;

    public SkinnedMeshAnimationEvaluator(MeshAsset asset, MeshAsset.Animation animation, MeshAsset.Node[] nodes, SkinnedMeshAnimator animator)
    {
        meshAsset = asset;

        this.animation = animation;
        this.nodes = nodes;
        this.animator = animator;

        timeBetweenFrames = 1 / (float)((asset?.frameRate ?? 0) == 0 ? 1 : asset.frameRate);
    }

    /// <summary>
    /// Evaluates the current animation frame
    /// </summary>
    public void Evaluate()
    {
        if (animation == null || meshAsset == null)
        {
            return;
        }

        animator.playTime += Time.deltaTime;

        updateTimer += Time.deltaTime;

        if(updateTimer < timeBetweenFrames)
        {
            return;
        }

        updateTimer = 0;

        var t = animator.playTime * animation.ticksPerSecond;
        var time = t % animation.duration;

        if (animator.repeat == false && t >= animation.duration)
        {
            time = animation.duration;
        }

        animator.playTime = time / animation.ticksPerSecond;

        for (var i = 0; i < animation.channels.Count; i++)
        {
            var channel = animation.channels[i];

            if (channel.nodeIndex < 0 || channel.nodeIndex >= nodes.Length)
            {
                continue;
            }

            var node = nodes[channel.nodeIndex];

            Vector3 GetVector3(List<MeshAsset.AnimationKey<Vector3>> keys, ref int last)
            {
                var outValue = Vector3.Zero;

                if (keys.Count > 0)
                {
                    var frame = time >= lastTime ? last : 0;

                    while (frame < keys.Count - 1)
                    {
                        if (time < keys[frame + 1].time)
                        {
                            break;
                        }

                        frame++;
                    }

                    if (frame >= keys.Count)
                    {
                        frame = 0;
                    }

                    var nextFrame = (frame + 1) % keys.Count;

                    var current = keys[frame];
                    var next = keys[nextFrame];

                    var timeDifference = next.time - current.time;

                    if (timeDifference < 0)
                    {
                        timeDifference += animation.duration;
                    }

                    if (timeDifference > 0)
                    {
                        outValue = Vector3.Lerp(current.value, next.value, (time - current.time) / timeDifference);
                    }
                    else
                    {
                        outValue = current.value;
                    }

                    last = frame;
                }

                return outValue;
            }

            Quaternion GetQuaternion(List<MeshAsset.AnimationKey<Quaternion>> keys, ref int last)
            {
                var outValue = Quaternion.Zero;

                if (keys.Count > 0)
                {
                    var frame = time >= lastTime ? last : 0;

                    while (frame < keys.Count - 1)
                    {
                        if (time < keys[frame + 1].time)
                        {
                            break;
                        }

                        frame++;
                    }

                    if (frame >= keys.Count)
                    {
                        frame = 0;
                    }

                    var nextFrame = (frame + 1) % keys.Count;

                    var current = keys[frame];
                    var next = keys[nextFrame];

                    var timeDifference = next.time - current.time;

                    if (timeDifference < 0)
                    {
                        timeDifference += animation.duration;
                    }

                    if (timeDifference > 0)
                    {
                        outValue = Quaternion.Slerp(current.value, next.value, (time - current.time) / timeDifference);
                    }
                    else
                    {
                        outValue = current.value;
                    }

                    last = frame;
                }

                return outValue;
            }

            if (lastPositionIndex.TryGetValue(i, out var positionIndex) == false)
            {
                lastPositionIndex.Add(i, 0);
            }

            if (lastScaleIndex.TryGetValue(i, out var scaleIndex) == false)
            {
                lastScaleIndex.Add(i, 0);
            }

            if (lastRotationIndex.TryGetValue(i, out var rotationIndex) == false)
            {
                lastRotationIndex.Add(i, 0);
            }

            var position = GetVector3(channel.positions, ref positionIndex);
            var scale = GetVector3(channel.scales, ref scaleIndex);
            var rotation = GetQuaternion(channel.rotations, ref rotationIndex);

            lastPositionIndex.AddOrSetKey(i, positionIndex);
            lastScaleIndex.AddOrSetKey(i, scaleIndex);
            lastRotationIndex.AddOrSetKey(i, rotationIndex);

            node.Transform = Math.TransformationMatrix(position, scale, rotation);
        }

        lastTime = time;

        SkinnedMeshRenderSystem.ApplyNodeTransform(animator.nodeCache, animator.transformCache);

        animator.shouldRender = true;
    }
}
