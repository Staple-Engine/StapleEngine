using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public class Renderer : Component
    {
        public bool enabled = true;

        public bool forceRenderingOff = false;

        public bool receiveShadows = true;

        public LayerMask renderingLayerMask;

        public int sortingOrder;

        public AABB bounds { get; protected set; }

        public AABB localBounds { get; protected set; }

        public bool isVisible { get; internal set; }
    }
}
