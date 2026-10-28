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
