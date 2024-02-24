using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple;

public class MeshAsset : IGuidAsset
{
    public class Bone
    {
        public string name;

        public Matrix4x4 offsetMatrix;
    }

    public class MeshInfo
    {
        public string name;
        public string materialGuid;
        public MeshAssetType type;
        public MeshTopology topology;
        public List<Vector3> vertices = new();
        public List<Vector3> normals = new();
        public List<Color> colors = new();
        public List<Vector3> tangents = new();
        public List<Vector3> bitangents = new();
        public List<Vector2> UV1 = new();
        public List<Vector2> UV2 = new();
        public List<Vector2> UV3 = new();
        public List<Vector2> UV4 = new();
        public List<Vector2> UV5 = new();
        public List<Vector2> UV6 = new();
        public List<Vector2> UV7 = new();
        public List<Vector2> UV8 = new();
        public List<int> indices = new();
        public List<Vector4> boneIndices = new();
        public List<Vector4> boneWeights = new();
        public List<Bone> bones = new();
        public AABB bounds;
    }

    public class Node
    {
        public string name;

        public Node parent;

        public List<Node> children = new();

        public Matrix4x4 originalTransform;

        public Matrix4x4 transform;

        public Matrix4x4 OriginalGlobalTransform
        {
            get
            {
                if (parent != null)
                {
                    return originalTransform * parent.OriginalGlobalTransform;
                }

                return originalTransform;
            }
        }

        public Matrix4x4 GlobalTransform
        {
            get
            {
                if(parent != null)
                {
                    return transform * parent.GlobalTransform;
                }

                return transform;
            }
        }

        public override string ToString()
        {
            return $"({name}, parent: {parent?.name ?? "None"})";
        }
    }

    public class AnimationKey<T>
    {
        public float time;
        public T value;
    }

    public class AnimationChannel
    {
        public Node node;

        public List<AnimationKey<Vector3>> positions = new();
        
        public List<AnimationKey<Vector3>> scales = new();

        public List<AnimationKey<Quaternion>> rotations = new();

        public Matrix4x4 TransformMatrix(float time)
        {
            T GetValue<T>(List<AnimationKey<T>> values)
            {
                T value = default;

                for(var i = 0; i < values.Count; i++)
                {
                    if (values[i].time > time)
                    {
                        return value;
                    }

                    value = values[i].value;
                }

                return value;
            }

            var position = GetValue(positions);
            var scale = GetValue(scales);
            var rotation = GetValue(rotations);

            return Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position);
        }
    }

    public class Animation
    {
        public string name;
        public float duration;
        public float ticksPerSecond;
        public List<AnimationChannel> channels = new();

        public float DurationRealtime => duration / ticksPerSecond;
    }

    public List<MeshInfo> meshes = new();
    public Node rootNode;
    public Dictionary<string, Node> nodes = new();
    public Matrix4x4 inverseTransform;
    public Dictionary<string, Animation> animations = new();

    public string Guid { get; set; }

    public Node GetNode(string name) => name != null && nodes.TryGetValue(name, out var node) ? node : null;

    public Animation GetAnimation(string name) => name != null && animations.TryGetValue(name, out var animation) ? animation : null;

    public static object Create(string guid)
    {
        return ResourceManager.instance.LoadMeshAsset(guid);
    }
}
