using Bgfx;
using System;
using System.Collections.Generic;
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

    private readonly List<RenderInfo> renderers = [];

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

        var matrix = new Transform()
        {
            Position = position,
            LocalRotation = rotation,
            LocalScale = scale,
        }.Matrix;

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

        material.ApplyProperties();

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
        var r = relatedComponent as MeshRenderer;

        if (r.mesh == null ||
            r.materials == null ||
            r.materials.Count == 0)
        {
            return;
        }

        for (var i = 0; i < r.materials.Count; i++)
        {
            if (r.materials[i]?.IsValid == false)
            {
                return;
            }
        }

        if (r.mesh.submeshes.Count > 0 && r.materials.Count != r.mesh.submeshes.Count)
        {
            return;
        }

        if(r.mesh.changed)
        {
            r.mesh.UploadMeshData();
        }

        r.localBounds = r.mesh.bounds;
        r.bounds = new AABB(transform.Position + r.mesh.bounds.center, r.mesh.bounds.extents * 2 * transform.Scale);
    }

    public void Process(Entity entity, Transform transform, IComponent relatedComponent,
        Camera activeCamera, Transform activeCameraTransform, ushort viewId)
    {
        var r = relatedComponent as MeshRenderer;

        if (r.mesh == null ||
            r.materials == null ||
            r.materials.Count == 0)
        {
            return;
        }

        for (var i = 0; i < r.materials.Count; i++)
        {
            if (r.materials[i]?.IsValid == false)
            {
                return;
            }
        }

        if (r.mesh.submeshes.Count > 0 && r.materials.Count != r.mesh.submeshes.Count)
        {
            return;
        }

        renderers.Add(new RenderInfo()
        {
            renderer = r,
            position = transform.Position,
            transform = transform.Matrix,
            viewID = viewId,
        });
    }

    public Type RelatedComponent()
    {
        return typeof(MeshRenderer);
    }

    public void Submit()
    {
        bgfx.StateFlags state = bgfx.StateFlags.WriteRgb |
            bgfx.StateFlags.WriteA |
            bgfx.StateFlags.WriteZ |
            bgfx.StateFlags.DepthTestLequal;

        foreach (var pair in renderers)
        {
            void DrawMesh(int index)
            {
                unsafe
                {
                    var transform = pair.transform;

                    _ = bgfx.set_transform(&transform, 1);
                }

                bgfx.set_state((ulong)(state |
                    pair.renderer.mesh.PrimitiveFlag() |
                    pair.renderer.materials[index].shader.BlendingFlag |
                    pair.renderer.materials[index].CullingFlag), 0);

                var material = pair.renderer.materials[index];

                material.ApplyProperties();

                pair.renderer.mesh.SetActive(index);

                material.DisableShaderKeyword(Shader.SkinningKeyword);

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

            if (pair.renderer.mesh.submeshes.Count == 0)
            {
                DrawMesh(0);
            }
            else
            {
                for (var i = 0; i < pair.renderer.mesh.submeshes.Count; i++)
                {
                    DrawMesh(i);
                }
            }
        }
    }
}
