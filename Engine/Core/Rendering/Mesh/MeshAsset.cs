using Staple.Internal;
using System.Collections.Generic;
using System.Numerics;

namespace Staple;

/// <summary>
/// Contains info on a mesh asset's data
/// </summary>
public class MeshAsset : IGuidAsset
{
    /// <summary>
    /// Bone info such as name and offset matrix
    /// </summary>
    public class Bone
    {
        public string name;

        public Matrix4x4 offsetMatrix;

        public override string ToString()
        {
            return name ?? "(invalid)";
        }
    }

    /// <summary>
    /// Data for drawing a submesh
    /// </summary>
    public class SubmeshInfo
    {
        public int startVertex;
        public int vertexCount;
        public int startIndex;
        public int indexCount;
    }

    /// <summary>
    /// Data for a mesh
    /// </summary>
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

    /// <summary>
    /// Mesh transform node
    /// </summary>
    public class Node
    {
        public string name;

        public Node parent;

        public List<Node> children = new();

        public List<int> meshIndices = new();

        private Matrix4x4 originalTransform;

        private Matrix4x4 transform;

        private bool changed = true;

        private Matrix4x4 globalMatrix;

        private Matrix4x4 originalGlobalMatrix;

        private void UpdateTransforms()
        {
            if (changed)
            {
                changed = false;

                if (parent != null)
                {
                    globalMatrix = transform * parent.GlobalTransform;
                    originalGlobalMatrix = originalTransform * parent.OriginalGlobalTransform;
                }
                else
                {
                    globalMatrix = transform;
                    originalGlobalMatrix = originalTransform;
                }
            }
        }

        public Matrix4x4 Transform
        {
            get => transform;

            set
            {
                transform = value;

                changed = true;
            }
        }

        public Matrix4x4 OriginalTransform
        {
            get => originalTransform;

            set
            {
                originalTransform = value;

                changed = true;
            }
        }

        public Matrix4x4 GlobalTransform
        {
            get
            {
                UpdateTransforms();

                return globalMatrix;
            }
        }

        public Matrix4x4 OriginalGlobalTransform
        {
            get
            {
                UpdateTransforms();

                return originalGlobalMatrix;
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
                parent = parent,
                meshIndices = meshIndices,
                changed = changed,
                transform = transform,
                globalMatrix = globalMatrix,
                originalTransform = originalTransform,
                originalGlobalMatrix = originalGlobalMatrix,
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

    /// <summary>
    /// Container for an animation key
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    public class AnimationKey<T>
    {
        public float time;
        public T value;
    }

    /// <summary>
    /// Animation channel, containing positions, scales, and rotations
    /// </summary>
    public class AnimationChannel
    {
        public Node node;

        public List<AnimationKey<Vector3>> positions = new();
        
        public List<AnimationKey<Vector3>> scales = new();

        public List<AnimationKey<Quaternion>> rotations = new();
    }

    /// <summary>
    /// Container for an animation, with its name, duration, and channels
    /// </summary>
    public class Animation
    {
        public string name;
        public float duration;
        public float ticksPerSecond;
        public List<AnimationChannel> channels = new();

        public float DurationRealtime => duration / ticksPerSecond;
    }

    /// <summary>
    /// List of each mesh in the asset
    /// </summary>
    public List<MeshInfo> meshes = new();

    /// <summary>
    /// The root node of the transform tree
    /// </summary>
    public Node rootNode;

    /// <summary>
    /// The inverse root node transform
    /// </summary>
    public Matrix4x4 inverseTransform;

    /// <summary>
    /// List of all animations
    /// </summary>
    public Dictionary<string, Animation> animations = new();

    /// <summary>
    /// 3D bounds of the mesh
    /// </summary>
    public AABB Bounds { get; internal set; }

    /// <summary>
    /// Asset GUID
    /// </summary>
    public string Guid { get; set; }

    /// <summary>
    /// Attempts to find a transform node with a specific name
    /// </summary>
    /// <param name="rootNode"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Node GetNode(Node rootNode, string name)
    {
        if (name == null)
        {
            return null;
        }

        return rootNode.GetNode(name);
    }

    /// <summary>
    /// Attempts to get a transform node
    /// </summary>
    /// <param name="rootNode">The root node to check</param>
    /// <param name="name">The name of the node</param>
    /// <param name="node">The node, which is valid if the return is true</param>
    /// <returns>Whether the node was found</returns>
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

    /// <summary>
    /// Attempts to get a transform node by name
    /// </summary>
    /// <param name="name">The name of the node</param>
    /// <returns>The node, or null</returns>
    public Node GetNode(string name) => GetNode(rootNode, name);

    /// <summary>
    /// Attempts to get a transform node by name
    /// </summary>
    /// <param name="name">The name fo the node</param>
    /// <param name="node">The node, which is valid if the return is true</param>
    /// <returns>Whether the node was found</returns>
    public bool TryGetNode(string name, out Node node) => TryGetNode(rootNode, name, out node);

    /// <summary>
    /// Attempts to get an animation by name
    /// </summary>
    /// <param name="name">The name of the animation</param>
    /// <returns>The animation, if found</returns>
    public Animation GetAnimation(string name) => name != null && animations.TryGetValue(name, out var animation) ? animation : null;

    /// <summary>
    /// Loads a Mesh Asset by guid
    /// </summary>
    /// <param name="guid">The guid to load</param>
    /// <returns>The mesh asset, or null</returns>
    public static object Create(string guid)
    {
        return ResourceManager.instance.LoadMeshAsset(guid);
    }
}
