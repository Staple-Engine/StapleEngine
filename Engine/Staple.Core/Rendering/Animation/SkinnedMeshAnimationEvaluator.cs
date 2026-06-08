using System;
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
            return !animator.repeat &&
                animator.playTime >= animation.duration;
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
    private int[] lastPositionIndex = [];

    /// <summary>
    /// Cache of last indices for rotations (channel index -> frame index)
    /// </summary>
    private int[] lastRotationIndex = [];

    /// <summary>
    /// Cache of last indices for scales (channel index -> frame index)
    /// </summary>
    private int[] lastScaleIndex = [];

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

        var frameRate = asset != null ? asset.syncAnimationToRefreshRate ? Screen.RefreshRate : asset.frameRate : 1;

        timeBetweenFrames = 1 / (float)frameRate;
    }

    /// <summary>
    /// Evaluates the current animation frame
    /// </summary>
    /// <returns>Returns true when the animation was updated</returns>
    public bool Evaluate()
    {
        if (animation == null || meshAsset == null)
        {
            return false;
        }

        animator.playTime += Time.deltaTime;

        updateTimer += Time.deltaTime;

        if(updateTimer < timeBetweenFrames)
        {
            return false;
        }

        updateTimer = 0;

        var t = animator.playTime;
        var time = t % animation.duration;

        if (!animator.repeat && t >= animation.duration)
        {
            time = animation.duration;
        }

        animator.playTime = time;

        if(lastPositionIndex.Length != animation.channels.Count)
        {
            Array.Resize(ref lastPositionIndex, animation.channels.Count);
        }

        if (lastScaleIndex.Length != animation.channels.Count)
        {
            Array.Resize(ref lastScaleIndex, animation.channels.Count);
        }

        if (lastRotationIndex.Length != animation.channels.Count)
        {
            Array.Resize(ref lastRotationIndex, animation.channels.Count);
        }

        for (var i = 0; i < animation.channels.Count; i++)
        {
            var channel = animation.channels[i];

            if (channel.nodeIndex < 0 || channel.nodeIndex >= nodes.Length)
            {
                continue;
            }

            Vector3 GetVector3(MeshAsset.AnimationKey<Vector3>[] keys, ref int last)
            {
                var outValue = Vector3.Zero;

                if (keys.Length > 0)
                {
                    var frame = time >= lastTime ? last : 0;

                    while (frame < keys.Length - 1)
                    {
                        if (time < keys[frame + 1].time)
                        {
                            break;
                        }

                        frame++;
                    }

                    if (frame >= keys.Length)
                    {
                        frame = 0;
                    }

                    var nextFrame = (frame + 1) % keys.Length;

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

            Quaternion GetQuaternion(MeshAsset.AnimationKey<Quaternion>[] keys, ref int last)
            {
                var outValue = Quaternion.Zero;

                if (keys.Length > 0)
                {
                    var frame = time >= lastTime ? last : 0;

                    while (frame < keys.Length - 1)
                    {
                        if (time < keys[frame + 1].time)
                        {
                            break;
                        }

                        frame++;
                    }

                    if (frame >= keys.Length)
                    {
                        frame = 0;
                    }

                    var nextFrame = (frame + 1) % keys.Length;

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

            var positionIndex = lastPositionIndex[i];
            var scaleIndex = lastScaleIndex[i];
            var rotationIndex = lastRotationIndex[i];

            var position = GetVector3(channel.positions, ref positionIndex);
            var scale = GetVector3(channel.scales, ref scaleIndex);
            var rotation = GetQuaternion(channel.rotations, ref rotationIndex);

            lastPositionIndex[i] = positionIndex;
            lastScaleIndex[i] = scaleIndex;
            lastRotationIndex[i] = rotationIndex;

            SkinnedMeshRenderSystem.ApplyNodeTransformQuick(channel.nodeIndex, position, rotation, scale, animator.transformCache);
        }

        lastTime = time;

        return true;
    }
}
