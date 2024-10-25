using Staple.Internal;
using System.Collections.Generic;
using System.Numerics;

namespace Staple;

/// <summary>
/// Contains info on a mesh asset's data
/// </summary>
public sealed class MeshAsset : IGuidAsset
{
    /// <summary>
    /// Bone info such as name and offset matrix
    /// </summary>
    public class Bone
    {
        /// <summary>
        /// Bone name
        /// </summary>
        public string name;

        /// <summary>
        /// Bone offset matrix
        /// </summary>
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
        /// <summary>
        /// The vertex to start rendering from
        /// </summary>
        public int startVertex;

        /// <summary>
        /// How many vertices to render
        /// </summary>
        public int vertexCount;

        /// <summary>
        /// The index to start rendering from
        /// </summary>
        public int startIndex;

        /// <summary>
        /// How many indices to render
        /// </summary>
        public int indexCount;
    }

    /// <summary>
    /// Data for a mesh
    /// </summary>
    public class MeshInfo
    {
        /// <summary>
        /// The mesh name
        /// </summary>
        public string name;

        /// <summary>
        /// The type of mesh
        /// </summary>
        public MeshAssetType type;

        /// <summary>
        /// The mesh topology (geometry type)
        /// </summary>
        public MeshTopology topology;

        /// <summary>
        /// The mesh vertices
        /// </summary>
        public Vector3[] vertices = [];

        /// <summary>
        /// The mesh normals
        /// </summary>
        public Vector3[] normals = [];

        /// <summary>
        /// The mesh colors
        /// </summary>
        public Color[] colors = [];

        /// <summary>
        /// The mesh tangents
        /// </summary>
        public Vector3[] tangents = [];

        /// <summary>
        /// The mesh bitangents
        /// </summary>
        public Vector3[] bitangents = [];

        /// <summary>
        /// The first UV stream
        /// </summary>
        public Vector2[] UV1 = [];

        /// <summary>
        /// The second UV stream
        /// </summary>
        public Vector2[] UV2 = [];

        /// <summary>
        /// The third UV stream
        /// </summary>
        public Vector2[] UV3 = [];

        /// <summary>
        /// The fourth UV stream
        /// </summary>
        public Vector2[] UV4 = [];

        /// <summary>
        /// The fifth UV stream
        /// </summary>
        public Vector2[] UV5 = [];

        /// <summary>
        /// The sixth UV stream
        /// </summary>
        public Vector2[] UV6 = [];

        /// <summary>
        /// The seventh UV stream
        /// </summary>
        public Vector2[] UV7 = [];

        /// <summary>
        /// The eighth UV stream
        /// </summary>
        public Vector2[] UV8 = [];

        /// <summary>
        /// The mesh indices
        /// </summary>
        public int[] indices = [];

        /// <summary>
        /// The bone indices
        /// </summary>
        public Vector4[] boneIndices = [];

        /// <summary>
        /// The bone weights
        /// </summary>
        public Vector4[] boneWeights = [];

        /// <summary>
        /// A list of bones per submesh
        /// </summary>
        public List<Bone[]> bones = [];

        /// <summary>
        /// The bounds of the mesh
        /// </summary>
        public AABB bounds;

        /// <summary>
        /// A list of submeshes
        /// </summary>
        public SubmeshInfo[] submeshes = [];

        /// <summary>
        /// The material GUIDs for the submeshes
        /// </summary>
        public string[] submeshMaterialGuids = [];

        /// <summary>
        /// The lighting to apply
        /// </summary>
        public MaterialLighting lighting;
    }

    /// <summary>
    /// Mesh transform node
    /// </summary>
    public class Node
    {
        /// <summary>
        /// The node name
        /// </summary>
        public string name;

        /// <summary>
        /// The parent of the node (if any)
        /// </summary>
        public Node parent;

        /// <summary>
        /// The child nodes
        /// </summary>
        public List<Node> children = [];

        /// <summary>
        /// The meshes that are in this node
        /// </summary>
        public List<int> meshIndices = [];

        /// <summary>
        /// Whether the values were changed (and need recalculation)
        /// </summary>
        private bool changed = true;

        /// <summary>
        /// Whether the transforms were changed (and need recalculation)
        /// </summary>
        private bool transformChanged = true;

        /// <summary>
        /// The local transform without animations
        /// </summary>
        private Matrix4x4 originalTransform;

        /// <summary>
        /// The current local transform
        /// </summary>
        private Matrix4x4 transform;

        /// <summary>
        /// The global transformation matrix
        /// </summary>
        private Matrix4x4 globalMatrix;

        /// <summary>
        /// The global transformation matrix without animations
        /// </summary>
        private Matrix4x4 originalGlobalMatrix;

        /// <summary>
        /// A list of names to nodes
        /// </summary>
        private readonly Dictionary<string, Node> cachedNodes = [];

        /// <summary>
        /// The local position
        /// </summary>
        private Vector3 position;

        /// <summary>
        /// The local position without animations
        /// </summary>
        private Vector3 originalPosition;

        /// <summary>
        /// The local scale
        /// </summary>
        private Vector3 scale;

        /// <summary>
        /// The local scale without animations
        /// </summary>
        private Vector3 originalScale;

        /// <summary>
        /// The local rotation
        /// </summary>
        private Quaternion rotation;

        /// <summary>
        /// The local rotation without animations
        /// </summary>
        private Quaternion originalRotation;

        /// <summary>
        /// Whether we need to calculate the original matrices
        /// </summary>
        private bool needsOriginalCalculation = true;

        /// <summary>
        /// Updates our transforms
        /// </summary>
        private void UpdateTransforms()
        {
            if(needsOriginalCalculation)
            {
                needsOriginalCalculation = false;

                if (parent != null)
                {
                    originalGlobalMatrix = originalTransform * parent.OriginalGlobalTransform;
                }
                else
                {
                    originalGlobalMatrix = originalTransform;
                }

                Matrix4x4.Decompose(originalTransform, out originalScale, out originalRotation, out originalPosition);
            }

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

            if(transformChanged)
            {
                transformChanged = false;

                Matrix4x4.Decompose(transform, out scale, out rotation, out position);
            }
        }

        /// <summary>
        /// The current local position
        /// </summary>
        public Vector3 Position
        {
            get
            {
                UpdateTransforms();

                return position;
            }
        }

        /// <summary>
        /// The current local position without animations
        /// </summary>
        public Vector3 OriginalPosition
        {
            get
            {
                UpdateTransforms();

                return originalPosition;
            }
        }

        /// <summary>
        /// The current local scale
        /// </summary>
        public Vector3 Scale
        {
            get
            {
                UpdateTransforms();

                return scale;
            }
        }

        /// <summary>
        /// The current local scale without animations
        /// </summary>
        public Vector3 OriginalScale
        {
            get
            {
                UpdateTransforms();

                return originalScale;
            }
        }

        /// <summary>
        /// The current local rotation
        /// </summary>
        public Quaternion Rotation
        {
            get
            {
                UpdateTransforms();

                return rotation;
            }
        }

        /// <summary>
        /// The current local rotation without animations
        /// </summary>
        public Quaternion OriginalRotation
        {
            get
            {
                UpdateTransforms();

                return originalRotation;
            }
        }

        /// <summary>
        /// The current local transform
        /// </summary>
        public Matrix4x4 Transform
        {
            get => transform;

            set
            {
                transform = value;

                changed = transformChanged = true;
            }
        }

        /// <summary>
        /// The current local transform without animations
        /// </summary>
        public Matrix4x4 OriginalTransform
        {
            get => originalTransform;

            set
            {
                originalTransform = value;

                needsOriginalCalculation = true;
            }
        }

        /// <summary>
        /// The current global transform
        /// </summary>
        public Matrix4x4 GlobalTransform
        {
            get
            {
                UpdateTransforms();

                return globalMatrix;
            }
        }

        /// <summary>
        /// The current global transform without animations
        /// </summary>
        public Matrix4x4 OriginalGlobalTransform
        {
            get
            {
                UpdateTransforms();

                return originalGlobalMatrix;
            }
        }

        /// <summary>
        /// Tries to get a node from a name
        /// </summary>
        /// <param name="name">The node name</param>
        /// <returns>The node, or null</returns>
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

        /// <summary>
        /// Makes a copy of this node
        /// </summary>
        /// <param name="parent">The parent node to set for this</param>
        /// <returns>The copy</returns>
        internal Node Clone(Node parent = null)
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
                originalPosition = originalPosition,
                originalRotation = originalRotation,
                originalScale = originalScale,
                position = position,
                rotation = rotation,
                scale = scale,
                transformChanged = transformChanged,
                needsOriginalCalculation = needsOriginalCalculation,
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
        /// <summary>
        /// The time at which this key is active
        /// </summary>
        public float time;

        /// <summary>
        /// The value of the key
        /// </summary>
        public T value;
    }

    /// <summary>
    /// Animation channel, containing positions, scales, and rotations
    /// </summary>
    public class AnimationChannel
    {
        /// <summary>
        /// The node this belongs to
        /// </summary>
        public Node node;

        /// <summary>
        /// The positions in this key
        /// </summary>
        public List<AnimationKey<Vector3>> positions = [];

        /// <summary>
        /// The scales in this key
        /// </summary>
        public List<AnimationKey<Vector3>> scales = [];

        /// <summary>
        /// The rotations in this key
        /// </summary>
        public List<AnimationKey<Quaternion>> rotations = [];
    }

    /// <summary>
    /// Container for an animation, with its name, duration, and channels
    /// </summary>
    public class Animation
    {
        /// <summary>
        /// The animation name
        /// </summary>
        public string name;

        /// <summary>
        /// The duration in ticks
        /// </summary>
        public float duration;

        /// <summary>
        /// How many ticks per second
        /// </summary>
        public float ticksPerSecond;

        /// <summary>
        /// The channels in this animation
        /// </summary>
        public List<AnimationChannel> channels = [];

        /// <summary>
        /// The duration in real time
        /// </summary>
        public float DurationRealtime => duration / ticksPerSecond;
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
    /// The inverse root node transform
    /// </summary>
    public Matrix4x4 inverseTransform;

    /// <summary>
    /// List of all animations
    /// </summary>
    public readonly Dictionary<string, Animation> animations = [];

    /// <summary>
    /// The lighting type for this mesh
    /// </summary>
    public MaterialLighting lighting;

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
