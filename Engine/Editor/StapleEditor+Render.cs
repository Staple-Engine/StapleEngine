using Bgfx;
using Hexa.NET.ImGuizmo;
using Staple.Internal;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Staple.Editor;

internal partial class StapleEditor
{
    private FrustumCuller frustumCuller = new();

    /// <summary>
    /// Renders the scene
    /// </summary>
    public void RenderScene()
    {
        bgfx.touch(SceneView);
        bgfx.touch(WireframeView);

        ImGuizmo.SetDrawlist();
        ImGuizmo.SetOrthographic(false);
        ImGuizmo.SetRect(0, 0, window.width, window.height);

        unsafe
        {
            var projection = Camera.Projection(default, camera);
            var view = cameraTransform.Matrix;

            Matrix4x4.Invert(view, out view);

            frustumCuller.Update(view, projection);

            bgfx.set_view_transform(SceneView, &view, &projection);
            bgfx.set_view_transform(WireframeView, &view, &projection);

            if (selectedEntity.IsValid &&
                selectedEntity.TryGetComponent<Transform>(out var selectedTransform))
            {
                ImGuizmo.Enable(true);

                unsafe
                {
                    float* snap = null;
                    float* localBound = null;
                    float[] snaps = [1, 1, 1];

                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        snap = (float*)Unsafe.AsPointer(ref snaps[0]);
                    }

                    var matrix = Math.TransformationMatrix(selectedTransform.Position, selectedTransform.Scale, selectedTransform.Rotation);

                    if (ImGuizmo.Manipulate(ref view, ref projection, ImGuizmoOperation.Translate, ImGuizmoMode.World, ref matrix, null, snap, localBound, snap))
                    {
                        if (Matrix4x4.Decompose(matrix, out var scale, out var rotation, out var position))
                        {
                            selectedTransform.Position = position;
                            selectedTransform.Scale = scale;
                            selectedTransform.Rotation = rotation;
                        }
                    }
                }
            }
            else
            {
                ImGuizmo.Enable(false);
            }
        }

        wireframeMaterial.SetVector4("cameraPosition", new Vector4(cameraTransform.Position, 1));

        var renderSystem = RenderSystem.Instance;

        foreach (var system in renderSystem.renderSystems)
        {
            system.Prepare();
        }

        if (World.Current != null)
        {
            var renderCamera = Scene.SortedCameras.FirstOrDefault()?.camera ?? camera;

            Scene.ForEach((Entity entity, ref Transform transform) =>
            {
                if(entity.Layer == LayerMask.NameToLayer(RenderTargetLayerName))
                {
                    return;
                }

                foreach(var system in renderSystem.renderSystems)
                {
                    var related = entity.GetComponent(system.RelatedComponent());

                    if (related != null)
                    {
                        system.Preprocess(entity, transform, related, renderCamera, cameraTransform);

                        if (related is Renderable renderable &&
                            renderable.enabled)
                        {
                            renderable.isVisible = frustumCuller.AABBTest(renderable.bounds) != FrustumAABBResult.Invisible || true; //TEMP: Figure out what's wrong with the frustum culler

                            if (renderable.isVisible && renderable.forceRenderingOff == false)
                            {
                                system.Process(entity, transform, related, renderCamera, cameraTransform, SceneView);
                            }

                            ReplaceEntityBodyIfNeeded(entity, transform, renderable.localBounds);
                        }
                        else if(related is not Renderable)
                        {
                            system.Process(entity, transform, related, renderCamera, cameraTransform, SceneView);
                        }
                    }
                }
            });

            if (cachedGizmoEditors.Count > 0)
            {
                var counter = 0;

                selectedEntity.IterateComponents((ref IComponent component) =>
                {
                    if(cachedGizmoEditors.TryGetValue(counter++, out var editor))
                    {
                        editor.OnGizmo(selectedEntity, selectedEntity.GetComponent<Transform>(), component);
                    }
                });
            }

            //Temporarily disabled because obtrusive
            /*
            Scene.IterateEntities((entity) =>
            {
                var transform = entity.GetComponent<Transform>();

                if(transform == null)
                {
                    return;
                }

                if(componentIcons.TryGetValue(entity, out var icon) == false)
                {
                    return;
                }

                componentIconMaterial ??= new Material(ResourceManager.instance.LoadMaterial("Materials/Sprite.mat"));

                componentIconMaterial.MainTexture = icon;

                MeshRenderSystem.DrawMesh(Mesh.Quad, transform.Position, Quaternion.Identity, Vector3.One, componentIconMaterial, WireframeView);
            });
            */
        }

        foreach (var system in renderSystem.renderSystems)
        {
            system.Submit();
        }
    }
}
