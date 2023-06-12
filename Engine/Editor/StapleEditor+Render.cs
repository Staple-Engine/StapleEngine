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
                Scene.current.world.ForEach((Entity entity, ref Transform transform) =>
                {
                    foreach(var system in renderSystem.renderSystems)
                    {
                        var related = Scene.current.world.GetComponent(entity, system.RelatedComponent());

                        if (related != null)
                        {
                            system.Preprocess(entity, transform, related);

                            if (related is Renderable renderable &&
                                renderable.enabled)
                            {
                                system.Process(entity, transform, related, SceneView);
                            }
                        }
                    }
                });
            }

            foreach(var system in renderSystem.renderSystems)
            {
                system.Submit();
            }
        }
    }
}
