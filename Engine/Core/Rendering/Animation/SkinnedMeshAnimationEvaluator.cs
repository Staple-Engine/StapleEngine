using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Channels;

namespace Staple;

internal class SkinnedMeshAnimationEvaluator
{
    public MeshAsset.Animation animation;
    public MeshAsset meshAsset;
    public SkinnedMeshAnimator animator;
    public MeshAsset.Node rootNode;

    public Action onFrameEvaluated;

    private float lastTime;
    private float updateTimer;

    private readonly float timeBetweenFrames;

    public bool FinishedPlaying
    {
        get
        {
            return animator.repeat == false &&
                animator.playTime * animation.ticksPerSecond >= animation.duration;
        }
    }

    public SkinnedMeshAnimationEvaluator(MeshAsset asset, MeshAsset.Animation animation, MeshAsset.Node rootNode, SkinnedMeshAnimator animator)
    {
        meshAsset = asset;

        this.animation = animation;
        this.rootNode = rootNode;
        this.animator = animator;

        timeBetweenFrames = 1 / (float)(asset.frameRate == 0 ? 1 : asset.frameRate);
    }

    public bool Bake(out Dictionary<float, Dictionary<string, MeshAsset.AnimationNodeState>> channels)
    {
        if (animation == null || meshAsset == null)
        {
            channels = default;
            return false;
        }

        var localData = new Dictionary<float, Dictionary<string, Matrix4x4>>();

        var lastPositionIndex = new Dictionary<int, int>();
        var lastRotationIndex = new Dictionary<int, int>();
        var lastScaleIndex = new Dictionary<int, int>();

        for(var i = 0; i < animation.channels.Count; i++)
        {
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
        }

        //TODO: Figure out how to speed this up
        for (var t = 0.0f; t < animation.duration * animation.ticksPerSecond; t += animation.ticksPerSecond / 1000)
        {
            var innerValue = new Dictionary<string, Matrix4x4>();

            for (var i = 0; i < animation.channels.Count; i++)
            {
                var channel = animation.channels[i];

                if (channel.node == null ||
                    MeshAsset.TryGetNode(rootNode, channel.node.name, out _) == false)
                {
                    continue;
                }

                Vector3 GetVector3(List<MeshAsset.AnimationKey<Vector3>> keys, ref int last)
                {
                    var outValue = Vector3.Zero;

                    if (keys.Count > 0)
                    {
                        var frame = t >= lastTime ? last : 0;

                        while (frame < keys.Count - 1)
                        {
                            if (t < keys[frame + 1].time)
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
                            outValue = Vector3.Lerp(current.value, next.value, (t - current.time) / timeDifference);
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
                        var frame = t >= lastTime ? last : 0;

                        while (frame < keys.Count - 1)
                        {
                            if (t < keys[frame + 1].time)
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
                            outValue = Quaternion.Slerp(current.value, next.value, (t - current.time) / timeDifference);
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

                var shouldAdd = positionIndex != lastPositionIndex[i] ||
                    scaleIndex != lastScaleIndex[i] ||
                    rotationIndex != lastRotationIndex[i];

                if (shouldAdd)
                {
                    lastPositionIndex.AddOrSetKey(i, positionIndex);
                    lastScaleIndex.AddOrSetKey(i, scaleIndex);
                    lastRotationIndex.AddOrSetKey(i, rotationIndex);

                    innerValue.Add(channel.node.name, Math.TransformationMatrix(position, scale, rotation));
                }
            }

            if(innerValue.Count > 0)
            {
                localData.Add(t, innerValue);
            }
        }

        channels = [];

        foreach(var timePair in localData)
        {
            channels.Add(timePair.Key, []);

            var finalData = channels[timePair.Key];

            //First: Assign all matrices
            foreach(var nodePair in timePair.Value)
            {
                if (MeshAsset.TryGetNode(rootNode, nodePair.Key, out var node) == false)
                {
                    continue;
                }

                node.Transform = nodePair.Value;
            }

            //Second: Get the result
            foreach (var nodePair in timePair.Value)
            {
                if (MeshAsset.TryGetNode(rootNode, nodePair.Key, out var node) == false)
                {
                    continue;
                }

                node.forceUpdateTransforms = true;

                node.UpdateTransforms();

                var global = node.GlobalTransform;

                Matrix4x4.Decompose(node.Transform, out var scale, out var rotation, out var translation);

                finalData.Add(nodePair.Key, new(global, translation, rotation, scale));
            }
        }

        return true;
    }

    public void Evaluate()
    {
        if (animation == null ||
            meshAsset == null)
        {
            return;
        }

        if(animation.bakedData == null)
        {
            Bake(out animation.bakedData);
        }

        updateTimer += Time.deltaTime;
        animator.playTime += Time.deltaTime;

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

        var target = t;
        var key = 0.0f;

        foreach (var frameKey in animation.bakedData.Keys)
        {
            if(target < frameKey)
            {
                break;
            }

            key = frameKey;
        }

        if(animation.bakedData.TryGetValue(key, out var channels) == false)
        {
            return;
        }

        foreach(var pair in channels)
        {
            if(MeshAsset.TryGetNode(rootNode, pair.Key, out var node) == false)
            {
                continue;
            }

            node.BakedTransform = pair.Value.transform;
            node.Position = pair.Value.position;
            node.Rotation = pair.Value.rotation;
            node.Scale = pair.Value.scale;
        }

        lastTime = time;

        onFrameEvaluated?.Invoke();
    }
}
