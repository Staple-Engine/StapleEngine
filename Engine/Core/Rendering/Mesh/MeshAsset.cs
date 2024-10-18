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
        public List<Vector3> vertices = [];
        public List<Vector3> normals = [];
        public List<Color> colors = [];
        public List<Vector3> tangents = [];
        public List<Vector3> bitangents = [];
        public List<Vector2> UV1 = [];
        public List<Vector2> UV2 = [];
        public List<Vector2> UV3 = [];
        public List<Vector2> UV4 = [];
        public List<Vector2> UV5 = [];
        public List<Vector2> UV6 = [];
        public List<Vector2> UV7 = [];
        public List<Vector2> UV8 = [];
        public List<int> indices = [];
        public List<Vector4> boneIndices = [];
        public List<Vector4> boneWeights = [];
        //One list per submesh
        public List<List<Bone>> bones = [];
        public AABB bounds;
        public List<SubmeshInfo> submeshes = [];
        public List<string> submeshMaterialGuids = [];
        public MeshLighting lighting;
    }

    /// <summary>
    /// Mesh transform node
    /// </summary>
    public class Node
    {
        public string name;

        public Node parent;

        public List<Node> children = [];

        public List<int> meshIndices = [];

        private Matrix4x4 originalTransform = Matrix4x4.Identity;

        internal Matrix4x4 transform = Matrix4x4.Identity;

        private bool changed = true;

        private bool transformChanged = true;

        internal Matrix4x4 globalMatrix = Matrix4x4.Identity;

        private readonly Dictionary<string, Node> cachedNodes = [];

        private Vector3 position;

        private Vector3 originalPosition;

        private Vector3 scale;

        private Vector3 originalScale;

        private Quaternion rotation;

        private Quaternion originalRotation;

        internal bool forceUpdateTransforms = false;

        internal void UpdateTransforms()
        {
            if (changed || forceUpdateTransforms)
            {
                changed = false;

                if (parent != null)
                {
                    if(forceUpdateTransforms)
                    {
                        parent.forceUpdateTransforms = true;
                    }

                    globalMatrix = transform * parent.GlobalTransform;
                }
                else
                {
                    globalMatrix = transform;
                }
            }

            if(transformChanged)
            {
                transformChanged = false;

                Matrix4x4.Decompose(transform, out scale, out rotation, out position);
            }
        }

        public Vector3 Position
        {
            get
            {
                UpdateTransforms();

                return position;
            }

            set
            {
                position = value;

                changed = transformChanged = false;
            }
        }

        public Vector3 OriginalPosition
        {
            get
            {
                UpdateTransforms();

                return originalPosition;
            }
        }

        public Vector3 Scale
        {
            get
            {
                UpdateTransforms();

                return scale;
            }

            set
            {
                scale = value;

                changed = transformChanged = false;
            }
        }

        public Vector3 OriginalScale
        {
            get
            {
                UpdateTransforms();

                return originalScale;
            }
        }

        public Quaternion Rotation
        {
            get
            {
                UpdateTransforms();

                return rotation;
            }

            set
            {
                rotation = value;

                changed = transformChanged = false;
            }
        }

        public Quaternion OriginalRotation
        {
            get
            {
                UpdateTransforms();

                return originalRotation;
            }
        }

        public Matrix4x4 Transform
        {
            get
            {
                UpdateTransforms();

                return transform;
            }

            set
            {
                transform = value;

                changed = transformChanged = false;
            }
        }

        public Matrix4x4 OriginalTransform
        {
            get => originalTransform;

            set
            {
                originalTransform = value;

                Matrix4x4.Decompose(originalTransform, out originalScale, out originalRotation, out originalPosition);
            }
        }

        public Matrix4x4 BakedTransform { get; set; } = Matrix4x4.Identity;

        public Matrix4x4 GlobalTransform
        {
            get
            {
                UpdateTransforms();

                return globalMatrix;
            }
        }

        public Node GetNode(string name)
        {
            if(cachedNodes.TryGetValue(name, out Node node))
            {
                return node;
            }

            Node Get(Node current)
            {
                if(current.name == name)
                {
                    return current;
                }

                foreach (var child in current.children)
                {
                    var result = Get(child);

                    if (result != null)
                    {
                        return result;
                    }
                }

                return null;
            }

            var result = Get(this);

            if(result != null)
            {
                cachedNodes.AddOrSetKey(name, result);
            }

            return result;
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

        public List<AnimationKey<Vector3>> positions = [];
        
        public List<AnimationKey<Vector3>> scales = [];

        public List<AnimationKey<Quaternion>> rotations = [];
    }

    /// <summary>
    /// Container for an animation, with its name, duration, and channels
    /// </summary>
    public class Animation
    {
        public string name;
        public float duration;
        public float ticksPerSecond;
        public List<AnimationChannel> channels = [];
        public Dictionary<float, Dictionary<string, AnimationNodeState>> bakedData = null;

        public float DurationRealtime => duration / ticksPerSecond;
    }

    /// <summary>
    /// Contains information about the state of an animation node
    /// </summary>
    public class AnimationNodeState(Matrix4x4 transform, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        public Matrix4x4 transform = transform;
        public Vector3 position = position;
        public Quaternion rotation = rotation;
        public Vector3 scale = scale;
    }

    /// <summary>
    /// List of each mesh in the asset
    /// </summary>
    public List<MeshInfo> meshes = [];

    /// <summary>
    /// The root node of the transform tree
    /// </summary>
    public Node rootNode;

    /// <summary>
    /// List of all animations
    /// </summary>
    public readonly Dictionary<string, Animation> animations = [];

    /// <summary>
    /// The lighting type for this mesh
    /// </summary>
    public MeshLighting lighting;

    /// <summary>
    /// The frame rate of the animations in this mesh
    /// </summary>
    public int frameRate = 30;

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
