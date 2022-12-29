using Bgfx;
using Staple.Internal;

namespace Staple.Editor
{
    internal partial class StapleEditor
    {
        public void RenderScene()
        {
            bgfx.touch(SceneView);
        }
    }
}
