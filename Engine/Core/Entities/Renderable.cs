namespace Staple
{
    /// <summary>
    /// Renderable base component
    /// </summary>
    public class Renderable : IComponent
    {
        /// <summary>
        /// Whether the render is enabled for this
        /// </summary>
        public bool enabled = true;

        /// <summary>
        /// Whether to force the rendering to be disabled
        /// </summary>
        public bool forceRenderingOff = false;

        /// <summary>
        /// Whether this receives shadows
        /// </summary>
        public bool receiveShadows = true;

        /// <summary>
        /// The rendering layer mask
        /// </summary>
        public LayerMask renderingLayerMask;

        /// <summary>
        /// The sorting layer for this renderer
        /// </summary>
        public uint sortingLayer;

        /// <summary>
        /// The sorting order for this renderer
        /// </summary>
        public int sortingOrder;

        /// <summary>
        /// The world-space bounds
        /// </summary>
        public AABB bounds { get; internal set; }

        /// <summary>
        /// The local bounds
        /// </summary>
        public AABB localBounds { get; internal set; }

        /// <summary>
        /// Whether this is visible
        /// </summary>
        public bool isVisible { get; internal set; }
    }
}
