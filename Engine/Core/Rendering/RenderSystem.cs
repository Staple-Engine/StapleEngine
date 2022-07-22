
using Bgfx;
using System;
using System.Linq;

namespace Staple
{
    class RenderSystem
    {
        public void Perform(Scene scene)
        {
            ushort viewID = 0;

            var cameras = scene.GetComponents<Camera>().OrderBy(x => x.depth);

            foreach(var camera in cameras)
            {
                camera.PrepareRender();

                unsafe
                {
                    var projection = camera.ProjectionMatrix;
                    var view = camera.Transform.Matrix;

                    bgfx.set_view_transform(viewID, &view.M11, &projection.M11);
                }

                viewID++;
            }
        }
    }
}
