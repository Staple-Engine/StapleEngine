using Bgfx;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.Internal;

/// <summary>
/// Skinned mesh render system
/// </summary>
public class SkinnedMeshRenderSystem : IRenderSystem
{
    /// <summary>
    /// Limit bones to 255
    /// </summary>
    internal const int MaxBones = 255;

    /// <summary>
    /// Infor for rendering
    /// </summary>
    private struct RenderInfo
    {
        /// <summary>
        /// The renderer
        /// </summary>
        public SkinnedMeshRenderer renderer;

        /// <summary>
        /// The current position of the object
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// The transform of the object
        /// </summary>
        public Matrix4x4 transform;

        /// <summary>
        /// The render view ID
        /// </summary>
        public ushort viewID;
    }

    private readonly List<RenderInfo> renderers = [];

    public void Destroy()
    {
    }

    public void Prepare()
    {
        renderers.Clear();
    }

    public void Preprocess(Entity entity, Transform transform, IComponent relatedComponent,
        Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Process(Entity entity, Transform transform, IComponent relatedComponent,
        Camera activeCamera, Transform activeCameraTransform, ushort viewId)
    {
        var renderer = relatedComponent as SkinnedMeshRenderer;

        if (renderer.mesh == null ||
            renderer.mesh.meshAsset == null ||
            renderer.mesh.meshAssetIndex < 0 ||
            renderer.mesh.meshAssetIndex >= renderer.mesh.meshAsset.meshes.Count ||
            renderer.materials == null ||
            renderer.materials.Count != renderer.mesh.submeshes.Count)
        {
            return;
        }

        for (var i = 0; i < renderer.materials.Count; i++)
        {
            if (renderer.materials[i]?.IsValid == false)
            {
                return;
            }
        }

        renderer.animator ??= new(entity, EntityQueryMode.Parent, false);

        renderers.Add(new RenderInfo()
        {
            renderer = renderer,
            position = transform.Position,
            transform = transform.Matrix,
            viewID = viewId,
        });
    }

    public Type RelatedComponent()
    {
        return typeof(SkinnedMeshRenderer);
    }

    public void Submit()
    {
        bgfx.StateFlags state = bgfx.StateFlags.WriteRgb |
            bgfx.StateFlags.WriteA |
            bgfx.StateFlags.WriteZ |
            bgfx.StateFlags.DepthTestLequal;

        foreach (var pair in renderers)
        {
            var renderer = pair.renderer;
            var animator = pair.renderer.animator.Length > 0 ? pair.renderer.animator[0] : null;
            var mesh = renderer.mesh;
            var meshAsset = mesh.meshAsset;
            var meshAssetMesh = meshAsset.meshes[mesh.meshAssetIndex];

            var useAnimator = animator != null && animator.evaluator != null;

            if(renderer.cachedBoneMatrices.Count != renderer.mesh.submeshes.Count)
            {
                renderer.cachedBoneMatrices.Clear();
                renderer.cachedNodes.Clear();
                renderer.cachedAnimatorNodes.Clear();
                
                for(var i = 0; i < renderer.mesh.submeshes.Count; i++)
                {
                    renderer.cachedBoneMatrices.Add([]);
                    renderer.cachedNodes.Add([]);
                    renderer.cachedAnimatorNodes.Add([]);
                }
            }

            for (var i = 0; i < renderer.mesh.submeshes.Count; i++)
            {
                var bones = meshAssetMesh.bones[i];

                if (bones.Length > MaxBones)
                {
                    Log.Warning($"Skipping skinned mesh render for {meshAssetMesh.name}: " +
                        $"Bone count of {bones.Length} exceeds limit of {MaxBones}, try setting split large meshes in the import settings!");

                    continue;
                }

                if (renderer.cachedBoneMatrices[i].Length != bones.Length)
                {
                    renderer.cachedBoneMatrices[i] = new Matrix4x4[bones.Length];
                    renderer.cachedNodes[i] = new MeshAsset.Node[bones.Length];

                    for(var j = 0; j < bones.Length; j++)
                    {
                        var bone = bones[j];

                        renderer.cachedNodes[i][j] = MeshAsset.TryGetNode(meshAsset.rootNode, bone.name, out var localNode) ?
                            localNode : null;

                        renderer.cachedBoneMatrices[i][j] = localNode != null ? bone.offsetMatrix * localNode.GlobalTransform : bone.offsetMatrix;
                    }
                }

                if(useAnimator && renderer.cachedAnimatorNodes[i].Length != bones.Length)
                {
                    renderer.cachedAnimatorNodes[i] = new MeshAsset.Node[bones.Length];

                    for(var j = 0; j < bones.Length; j++)
                    {
                        renderer.cachedAnimatorNodes[i][j] = MeshAsset.TryGetNode(animator.evaluator.rootNode, bones[j].name, out var localNode) ?
                            localNode : null;
                    }
                }

                var boneMatrices = renderer.cachedBoneMatrices[i];

                if (animator == null || animator.shouldRender)
                {
                    for (var j = 0; j < boneMatrices.Length; j++)
                    {
                        var bone = bones[j];

                        Matrix4x4 globalTransform;

                        if (useAnimator && renderer.cachedAnimatorNodes[i][j] is MeshAsset.Node localNode)
                        {
                            globalTransform = localNode.GlobalTransform;
                        }
                        else if (renderer.cachedNodes[i][j] is MeshAsset.Node node)
                        {
                            globalTransform = node.GlobalTransform;
                        }
                        else
                        {
                            globalTransform = Matrix4x4.Identity;
                        }

                        boneMatrices[j] = bone.offsetMatrix * globalTransform;
                    }
                }

                unsafe
                {
                    var transform = pair.transform;

                    _ = bgfx.set_transform(&transform, 1);
                }

                bgfx.set_state((ulong)(state |
                    renderer.mesh.PrimitiveFlag() |
                    renderer.materials[i].shader.BlendingFlag |
                    renderer.materials[i].CullingFlag), 0);

                var material = renderer.materials[i];

                material.ApplyProperties();

                material.shader.SetMatrix4x4(material.GetShaderHandle("u_boneMatrices"), boneMatrices);

                renderer.mesh.SetActive(i);

                material.EnableShaderKeyword(Shader.SkinningKeyword);

                var lightSystem = RenderSystem.Instance.Get<LightSystem>();

                lightSystem?.ApplyMaterialLighting(material, pair.renderer.lighting);

                var program = material.ShaderProgram;

                if (program.Valid)
                {
                    lightSystem?.ApplyLightProperties(pair.position, pair.transform, material,
                        RenderSystem.CurrentCamera.Item2.Position, pair.renderer.lighting);

                    bgfx.submit(pair.viewID, program, 0, (byte)bgfx.DiscardFlags.All);
                }
                else
                {
                    bgfx.discard((byte)bgfx.DiscardFlags.All);
                }
            }
        }
    }

    /// <summary>
    /// Gets all animation nodes for a parent transform
    /// </summary>
    /// <param name="nodeCache">A cache to store the nodes</param>
    /// <param name="rootNode">The root node</param>
    public static void GatherNodes(Dictionary<string, MeshAsset.Node> nodeCache, MeshAsset.Node rootNode)
    {
        if (nodeCache == null)
        {
            return;
        }

        nodeCache.Clear();

        void GatherNodes(MeshAsset.Node node)
        {
            if (node == null)
            {
                return;
            }

            nodeCache.AddOrSetKey(node.name, node);

            foreach (var child in node.children)
            {
                GatherNodes(child);
            }
        }

        GatherNodes(rootNode);
    }

    /// <summary>
    /// Gets all transforms related to animation nodes
    /// </summary>
    /// <param name="parent">The parent transform</param>
    /// <param name="transformCache">The transform cache</param>
    /// <param name="rootNode">The root node</param>
    public static void GatherNodeTransforms(Transform parent, Dictionary<string, Transform> transformCache, MeshAsset.Node rootNode)
    {
        if (parent == null || transformCache == null)
        {
            return;
        }

        transformCache.Clear();

        void GatherNodes(MeshAsset.Node node)
        {
            if (node == null)
            {
                return;
            }

            var childTransform = parent.SearchChild(node.name);

            if (childTransform == null)
            {
                foreach (var child in node.children)
                {
                    GatherNodes(child);
                }

                return;
            }

            transformCache.AddOrSetKey(node.name, childTransform);

            foreach (var child in node.children)
            {
                GatherNodes(child);
            }
        }

        GatherNodes(rootNode);
    }

    /// <summary>
    /// Applies the transforms of a node cache into its related entity transforms
    /// </summary>
    /// <param name="nodeCache">The node cache</param>
    /// <param name="transformCache">The transform cache</param>
    /// <param name="original">Whether we want the original transforms (before animating)</param>
    public static void ApplyNodeTransform(Dictionary<string, MeshAsset.Node> nodeCache, Dictionary<string, Transform> transformCache, bool original = false)
    {
        foreach (var pair in transformCache)
        {
            if(nodeCache.TryGetValue(pair.Key, out var node) == false)
            {
                continue;
            }

            pair.Value.LocalPosition = original ? node.OriginalPosition : node.Position;
            pair.Value.LocalRotation = original ? node.OriginalRotation : node.Rotation;
            pair.Value.LocalScale = original ? node.OriginalScale : node.Scale;
        }
    }

    /// <summary>
    /// Applies transforms to nodes. This lets you override the animation transforms.
    /// </summary>
    /// <param name="nodeCache">The node cache</param>
    /// <param name="transformCache">The transform cache</param>
    public static void ApplyTransformsToNodes(Dictionary<string, MeshAsset.Node> nodeCache, Dictionary<string, Transform> transformCache)
    {
        foreach(var pair in nodeCache)
        {
            if(transformCache.TryGetValue(pair.Key, out var transform) == false)
            {
                continue;
            }

            pair.Value.Transform = Math.TransformationMatrix(transform.LocalPosition, transform.LocalScale, transform.LocalRotation);
        }
    }
}
