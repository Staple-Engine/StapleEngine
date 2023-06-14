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
                var projection = Camera.Projection(null, Entity.Empty, camera);
                var view = cameraTransform.Matrix;

                Matrix4x4.Invert(view, out view);

                bgfx.set_view_transform(SceneView, &view, &projection);
            }

            if(Scene.current?.world != null)
            {
                var mouseRay = new Ray(Camera.ScreenPointToWorld(Input.MousePosition, Scene.current.world, Entity.Empty, camera, cameraTransform), cameraTransform.Forward);

                //TODO: Fix this
                if (Physics.RayCast3D(mouseRay, out var body, out _))
                {
                    Log.Info($"Hit {Scene.current.world.GetEntityName(body.Entity)}");
                }

                Scene.current.world.ForEach((Entity entity, ref Transform transform) =>
                {
                    foreach(var system in renderSystem.renderSystems)
                    {
                        var related = Scene.current.world.GetComponent(entity, system.RelatedComponent());

                        if (related != null)
                        {
                            system.Preprocess(entity, transform, related);

                            if (related is Renderable renderable)
                            {
                                system.Process(entity, transform, related, SceneView);

                                ReplaceEntityBodyIfNeeded(entity, transform, renderable.localBounds);
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
