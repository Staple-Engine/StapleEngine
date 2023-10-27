using Bgfx;
using ImGuiNET;
using Staple.Internal;
using System.Linq;
using System.Numerics;

namespace Staple.Editor
{
    internal partial class StapleEditor
    {
        public void RenderScene()
        {
            bgfx.touch(SceneView);
            bgfx.touch(WireframeView);

            unsafe
            {
                var projection = Camera.Projection(Scene.current?.world, Entity.Empty, camera);
                var view = cameraTransform.Matrix;

                Matrix4x4.Invert(view, out view);

                bgfx.set_view_transform(SceneView, &view, &projection);
                bgfx.set_view_transform(WireframeView, &view, &projection);
            }

            wireframeMaterial.SetVector4("cameraPosition", new Vector4(cameraTransform.Position, 1));

            foreach (var system in renderSystem.renderSystems)
            {
                system.Prepare();
            }

            if (Scene.current?.world != null)
            {
                var renderCamera = Scene.current.world.SortedCameras.FirstOrDefault()?.camera ?? camera;

                Scene.current.world.ForEach((Entity entity, bool enabled, ref Transform transform) =>
                {
                    if(enabled == false)
                    {
                        return;
                    }

                    foreach(var system in renderSystem.renderSystems)
                    {
                        var related = Scene.current.world.GetComponent(entity, system.RelatedComponent());

                        if (related != null)
                        {
                            system.Preprocess(Scene.current.world, entity, transform, related, renderCamera, cameraTransform);

                            if (related is Renderable renderable)
                            {
                                system.Process(Scene.current.world, entity, transform, related, renderCamera, cameraTransform, SceneView);

                                ReplaceEntityBodyIfNeeded(entity, transform, renderable.localBounds);
                            }
                        }
                    }
                });

                //Temporarily disabled due to ImGui issues when clicking dropdowns
                /*
                if(Input.GetMouseButtonDown(MouseButton.Left) && mouseIsHoveringImGui == false)
                {
                    var ray = Camera.ScreenPointToRay(Input.MousePosition, Scene.current.world, Entity.Empty, camera, cameraTransform);

                    if (Physics3D.Instance.RayCast(ray, out var body, out _, PhysicsTriggerQuery.Ignore, 1000))
                    {
                        SetSelectedEntity(body.Entity);
                    }
                    else
                    {
                        SetSelectedEntity(Entity.Empty);
                    }
                }
                */

                if (cachedGizmoEditors.Count > 0)
                {
                    var counter = 0;

                    Scene.current.world.IterateComponents(selectedEntity, (ref IComponent component) =>
                    {
                        if(cachedGizmoEditors.TryGetValue(counter++, out var editor))
                        {
                            editor.OnGizmo(selectedEntity, Scene.current.world.GetComponent<Transform>(selectedEntity), component);
                        }
                    });
                }

                Scene.current.world.Iterate((entity) =>
                {
                    var transform = Scene.current.world.GetComponent<Transform>(entity);

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
            }

            foreach (var system in renderSystem.renderSystems)
            {
                system.Submit();
            }
        }
    }
}
