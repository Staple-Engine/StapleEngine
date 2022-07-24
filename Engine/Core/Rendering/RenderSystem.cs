
using Bgfx;
using System;
using System.Linq;

namespace Staple
{
    class RenderSystem
    {
        public bool Perform(Scene scene)
        {
            ushort viewID = 1;

            var cameras = scene.GetComponents<Camera>().OrderBy(x => x.depth);

            bool performed = false;

            foreach(var camera in cameras)
            {
                performed = true;

                camera.PrepareRender(viewID);

                unsafe
                {
                    var projection = camera.ProjectionMatrix;
                    var view = camera.Transform.Matrix;

                    bgfx.set_view_transform(viewID, &view.M11, &projection.M11);
                }

                bgfx.touch(viewID);

                viewID++;
            }

            return performed;
        }
    }
}
