using Staple.Internal;

namespace Staple;

/// <summary>
/// Renderable base component
/// </summary>
[AbstractComponent]
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
    /// The sorting layer for this renderer
    /// </summary>
    [SortingLayer]
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

    /// <summary>
    /// Whether to override the lighting for this renderer
    /// </summary>
    public bool overrideLighting = false;

    /// <summary>
    /// Lighting mode
    /// </summary>
    public MaterialLighting lighting = MaterialLighting.Unlit;

    /// <summary>
    /// Whether this has been culled by another system
    /// </summary>
    internal CullingState cullingState = CullingState.None;
}
