namespace Staple
{
    public class Renderable : IComponent
    {
        public bool enabled = true;

        public bool forceRenderingOff = false;

        public bool receiveShadows = true;

        public LayerMask renderingLayerMask;

        public int sortingOrder;

        public AABB bounds { get; internal set; }

        public AABB localBounds { get; internal set; }

        public bool isVisible { get; internal set; }
    }
}
