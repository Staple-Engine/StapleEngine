using Staple.Internal;
using System.Collections.Generic;
using System.Numerics;

namespace Staple;

public class MeshAsset : IGuidAsset
{
    public class Bone
    {
        public string name;

        public Matrix4x4 offsetMatrix;

        public override string ToString()
        {
            return name ?? "(invalid)";
        }
    }

    public class SubmeshInfo
    {
        public int startVertex;
        public int vertexCount;
        public int startIndex;
        public int indexCount;
    }

    public class MeshInfo
    {
        public string name;
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
        //One list per submesh
        public List<List<Bone>> bones = new();
        public AABB bounds;
        public List<SubmeshInfo> submeshes = new();
        public List<string> submeshMaterialGuids = new();
    }

    public class Node
    {
        public string name;

        public Node parent;

        public List<Node> children = new();

        public List<int> meshIndices = new();

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

        public Node GetNode(string name)
        {
            if(this.name == name)
            {
                return this;
            }

            foreach(var child in children)
            {
                var result = child.GetNode(name);

                if(result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public Node Clone(Node parent = null)
        {
            var result = new Node()
            {
                name = name,
                originalTransform = originalTransform,
                transform = transform,
                parent = parent,
                meshIndices = meshIndices,
            };

            foreach(var child in children)
            {
                result.children.Add(child.Clone(result));
            }

            return result;
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
    public Matrix4x4 inverseTransform;
    public Dictionary<string, Animation> animations = new();

    public string Guid { get; set; }

    public static Node GetNode(Node rootNode, string name)
    {
        if (name == null)
        {
            return null;
        }

        return rootNode.GetNode(name);
    }

    public static bool TryGetNode(Node rootNode, string name, out Node node)
    {
        if (name == null)
        {
            node = null;

            return false;
        }

        node = rootNode.GetNode(name);

        return node != null;
    }

    public Node GetNode(string name) => GetNode(rootNode, name);

    public bool TryGetNode(string name, out Node node) => TryGetNode(rootNode, name, out node);

    public Animation GetAnimation(string name) => name != null && animations.TryGetValue(name, out var animation) ? animation : null;

    public static object Create(string guid)
    {
        return ResourceManager.instance.LoadMeshAsset(guid);
    }
}
