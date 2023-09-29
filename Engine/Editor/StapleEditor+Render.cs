using Bgfx;
using System.Numerics;

namespace Staple.Editor
{
    internal partial class StapleEditor
    {
        public void RenderScene()
        {
            bgfx.touch(SceneView);

            unsafe
            {
                var projection = Camera.Projection(Scene.current?.world, Entity.Empty, camera);
                var view = cameraTransform.Matrix;

                Matrix4x4.Invert(view, out view);

                bgfx.set_view_transform(SceneView, &view, &projection);
            }

            foreach (var system in renderSystem.renderSystems)
            {
                system.Prepare();
            }

            if (Scene.current?.world != null)
            {
                Scene.current.world.ForEach((Entity entity, ref Transform transform) =>
                {
                    foreach(var system in renderSystem.renderSystems)
                    {
                        var related = Scene.current.world.GetComponent(entity, system.RelatedComponent());

                        if (related != null)
                        {
                            system.Preprocess(Scene.current.world, entity, transform, related);

                            if (related is Renderable renderable)
                            {
                                system.Process(Scene.current.world, entity, transform, related, SceneView);

                                ReplaceEntityBodyIfNeeded(entity, transform, renderable.localBounds);

                                MeshRenderSystem.DrawMesh(Mesh.Cube, transform.Position, transform.Rotation, renderable.localBounds.extents, debugHighlightMaterial, SceneView);
                            }
                        }
                    }
                });
            }

            foreach (var system in renderSystem.renderSystems)
            {
                system.Submit();
            }
        }
    }
}
