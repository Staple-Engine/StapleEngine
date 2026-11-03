using Staple.Internal;
using System;
using System.Collections.Generic;

namespace Staple;

/// <summary>
/// Renderable base component
/// </summary>
[AbstractComponent]
public partial class Renderable : VersionableComponent
{
    /// <summary>
    /// Whether to force the rendering to be disabled
    /// </summary>
    public partial bool forceRenderingOff { get; set; }

    /// <summary>
    /// Whether this receives shadows
    /// </summary>
    public partial bool receiveShadows { get; set; }

    /// <summary>
    /// The sorting layer for this renderer
    /// </summary>
    [SortingLayer]
    public partial uint sortingLayer { get; set; }

    /// <summary>
    /// The sorting order for this renderer
    /// </summary>
    public partial int sortingOrder { get; set; }

    /// <summary>
    /// The world-space bounds
    /// </summary>
    public partial AABB bounds { get; set; }

    /// <summary>
    /// The local bounds
    /// </summary>
    public AABB localBounds { get; set; }

    /// <summary>
    /// Whether this is visible
    /// </summary>
    public bool isVisible { get; internal set; }

    /// <summary>
    /// Whether this <see cref="Renderable"/> requires precise culling.
    /// If this <see cref="Renderable"/> requires a drawcall, you should set this to true to ensure it doesn't render unless needed.
    /// </summary>
    /// <remarks>
    /// If this renderer requires a drawcall each time, this should always be true, unless you notice the performance dropping a lot.
    /// Otherwise, if you can do instancing or <see cref="RenderSystem.SubmitStatic(RenderState, Span{MultidrawEntry})"/>, it should have much less
    /// of a cost to render per object, and probably doesn't need this set up. Always verify performance first!
    /// </remarks>
    public bool requiresPreciseCulling { get; protected set; }

    /// <summary>
    /// The materials for this renderable
    /// </summary>
    public partial List<Material> materials { get; set; }

    /// <summary>
    /// Whether this has been culled by another system
    /// </summary>
    internal CullingState cullingState = CullingState.None;

    /// <summary>
    /// Gets the current state of all materials contained in this renderable,
    /// to verify whether the render queue requires recalculation
    /// </summary>
    internal int MaterialState
    {
        get
        {
            var hashCode = new HashCode();

            foreach(var material in materials)
            {
                hashCode.Add(material?.StateHash ?? 0);
            }

            return hashCode.ToHashCode();
        }
    }

    public Renderable()
    {
        receiveShadows = true;
        materials = [];
    }
}
