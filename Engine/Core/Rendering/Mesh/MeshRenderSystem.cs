using Bgfx;
using System;
using System.Numerics;

namespace Staple.Internal;

/// <summary>
/// Mesh render system
/// </summary>
public sealed class MeshRenderSystem : IRenderSystem
{
    /// <summary>
    /// Contains info on something that is meant to be rendered
    /// </summary>
    private struct RenderInfo
    {
        /// <summary>
        /// The mesh renderer to render
        /// </summary>
        public MeshRenderer renderer;

        /// <summary>
        /// The current transform
        /// </summary>
        public Matrix4x4 transform;

        /// <summary>
        /// The current position
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// The view ID to render to
        /// </summary>
        public ushort viewID;
    }

    private readonly ExpandableContainer<RenderInfo> renderers = new();

    /// <summary>
    /// Renders a mesh
    /// </summary>
    /// <param name="mesh">The mesh</param>
    /// <param name="position">The position of the mesh</param>
    /// <param name="rotation">The rotation of the mesh</param>
    /// <param name="scale">The scale of the mesh</param>
    /// <param name="material">The material to use</param>
    /// <param name="lighting">The lighting model to use</param>
    /// <param name="viewID">The view ID to render to</param>
    public static void RenderMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Material material, MaterialLighting lighting, ushort viewID)
    {
        if(mesh == null ||
            material == null ||
            material.IsValid == false)
        {
            return;
        }

        if(mesh.changed)
        {
            mesh.UploadMeshData();
        }

        var matrix = Math.TransformationMatrix(position, scale, rotation);

        bgfx.StateFlags state = bgfx.StateFlags.WriteRgb |
            bgfx.StateFlags.WriteA |
            bgfx.StateFlags.WriteZ |
            bgfx.StateFlags.DepthTestLequal |
            mesh.PrimitiveFlag() |
            material.shader.BlendingFlag |
            material.CullingFlag;

        unsafe
        {
            _ = bgfx.set_transform(&matrix, 1);
        }

        bgfx.set_state((ulong)state, 0);

        material.ApplyProperties(Material.ApplyMode.All);

        mesh.SetActive();

        material.DisableShaderKeyword(Shader.SkinningKeyword);

        var lightSystem = RenderSystem.Instance.Get<LightSystem>();

        lightSystem?.ApplyMaterialLighting(material, lighting);

        var program = material.ShaderProgram;

        if (program.Valid)
        {
            lightSystem?.ApplyLightProperties(position, matrix, material, RenderSystem.CurrentCamera.Item2.Position, lighting);

            bgfx.submit(viewID, program, 0, (byte)bgfx.DiscardFlags.All);
        }
        else
        {
            bgfx.discard((byte)bgfx.DiscardFlags.All);
        }
    }

    public void Startup()
    {
    }

    public void Shutdown()
    {
    }

    public void Prepare()
    {
    }

    public void Preprocess((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform)
    {
        foreach (var (_, transform, relatedComponent) in entities)
        {
            var r = relatedComponent as MeshRenderer;

            if (r.isVisible == false ||
                r.mesh == null ||
                r.materials == null ||
                r.materials.Count == 0)
            {
                continue;
            }

            for (var i = 0; i < r.materials.Count; i++)
            {
                if (r.materials[i]?.IsValid == false)
                {
                    continue;
                }
            }

            if (r.mesh.submeshes.Count > 0 && r.materials.Count != r.mesh.submeshes.Count)
            {
                continue;
            }

            if (r.mesh.changed)
            {
                r.mesh.UploadMeshData();
            }

            r.localBounds = r.mesh.bounds;
            r.bounds = new AABB(transform.Position + r.mesh.bounds.center, r.mesh.bounds.extents * 2 * transform.Scale);
        }
    }

    public void Process((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform, ushort viewId)
    {
        renderers.Clear();

        foreach (var (_, transform, relatedComponent) in entities)
        {
            var r = relatedComponent as MeshRenderer;

            if (r.isVisible == false ||
                r.mesh == null ||
                r.materials == null ||
                r.materials.Count == 0)
            {
                continue;
            }

            for (var i = 0; i < r.materials.Count; i++)
            {
                if (r.materials[i]?.IsValid == false)
                {
                    continue;
                }
            }

            if (r.mesh.submeshes.Count > 0 && r.materials.Count != r.mesh.submeshes.Count)
            {
                continue;
            }

            renderers.Add(new()
            {
                renderer = r,
                position = transform.Position,
                transform = transform.Matrix,
                viewID = viewId
            });
        }
    }

    public Type RelatedComponent()
    {
        return typeof(MeshRenderer);
    }

    public void Submit()
    {
        bgfx.discard((byte)bgfx.DiscardFlags.All);

        var state = bgfx.StateFlags.WriteRgb |
            bgfx.StateFlags.WriteA |
            bgfx.StateFlags.WriteZ |
            bgfx.StateFlags.DepthTestLequal;

        Material lastMaterial = null;
        MaterialLighting lastLighting = MaterialLighting.Unlit;

        foreach(var pair in renderers.Contents)
        {
            void DrawMesh(int index)
            {
                var material = pair.renderer.materials[index];

                var needsChange = lastMaterial?.Guid.GetHashCode() != material?.Guid?.GetHashCode() ||
                    lastLighting != pair.renderer.lighting;

                if (needsChange)
                {
                    bgfx.discard((byte)bgfx.DiscardFlags.All);

                    lastMaterial = material;
                    lastLighting = pair.renderer.lighting;

                    material.DisableShaderKeyword(Shader.SkinningKeyword);

                    var lightSystem = RenderSystem.Instance.Get<LightSystem>();

                    lightSystem?.ApplyMaterialLighting(material, pair.renderer.lighting);

                    if(material.ShaderProgram.Valid == false)
                    {
                        return;
                    }

                    bgfx.set_state((ulong)(state |
                        pair.renderer.mesh.PrimitiveFlag() |
                        material.shader.BlendingFlag |
                        material.CullingFlag), 0);

                    material.ApplyProperties(Material.ApplyMode.All);

                    lightSystem?.ApplyLightProperties(pair.position, pair.transform, material,
                        RenderSystem.CurrentCamera.Item2.Position, pair.renderer.lighting);
                }

                unsafe
                {
                    var transform = pair.transform;

                    _ = bgfx.set_transform(&transform, 1);
                }

                pair.renderer.mesh.SetActive(index);

                var program = material.ShaderProgram;

                var flags = bgfx.DiscardFlags.VertexStreams |
                    bgfx.DiscardFlags.IndexBuffer |
                    bgfx.DiscardFlags.Transform;

                bgfx.submit(pair.viewID, program, 0, (byte)flags);
            }

            if (pair.renderer.mesh.submeshes.Count == 0)
            {
                DrawMesh(0);
            }
            else
            {
                for (var j = 0; j < pair.renderer.mesh.submeshes.Count; j++)
                {
                    DrawMesh(j);
                }
            }
        }
    }
}
