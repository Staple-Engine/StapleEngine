using Staple.Internal;
using System;
using System.Collections.Generic;

namespace Staple;

/// <summary>
/// Renderable base component
/// </summary>
[AbstractComponent]
public class Renderable : Component, IComponentVersion
{
    /// <summary>
    /// The version state of this renderable. Should be updated as you change properties like bounds.
    /// </summary>
    public ulong Version { get; protected set; }

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
    public AABB bounds;

    /// <summary>
    /// The local bounds
    /// </summary>
    public AABB localBounds;

    /// <summary>
    /// Whether this is visible
    /// </summary>
    public bool isVisible { get; internal set; }

    /// <summary>
    /// The materials for this renderable
    /// </summary>
    public List<Material> materials = [];

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

    /// <summary>
    /// Updates the <see cref="bounds"/> of this renderable if they changed
    /// </summary>
    /// <param name="bounds">The new bounds</param>
    public void UpdateBounds(AABB bounds)
    {
        if(bounds != this.bounds)
        {
            Version++;

            this.bounds = bounds;
        }
    }
}
