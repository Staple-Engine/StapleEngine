using Bgfx;
using Staple.Internal;
using System.Linq;
using System.Numerics;

namespace Staple.Editor;

internal partial class StapleEditor
{
    private FrustumCuller frustumCuller = new();

    public void RenderScene()
    {
        bgfx.touch(SceneView);
        bgfx.touch(WireframeView);

        unsafe
        {
            var projection = Camera.Projection(default, camera);
            var view = cameraTransform.Matrix;

            Matrix4x4.Invert(view, out view);

            frustumCuller.Update(view, projection);

            bgfx.set_view_transform(SceneView, &view, &projection);
            bgfx.set_view_transform(WireframeView, &view, &projection);
        }

        wireframeMaterial.SetVector4("cameraPosition", new Vector4(cameraTransform.Position, 1));

        foreach (var system in renderSystem.renderSystems)
        {
            system.Prepare();
        }

        if (World.Current != null)
        {
            var renderCamera = Scene.SortedCameras.FirstOrDefault()?.camera ?? camera;

            Scene.ForEach((Entity entity, bool enabled, ref Transform transform) =>
            {
                if(enabled == false)
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
                            renderable.isVisible = frustumCuller.AABBTest(renderable.bounds) != FrustumAABBResult.Invisible;

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
