namespace Staple;

/// <summary>
/// UI Element base class
/// </summary>
public abstract class UIElement : IComponent
{
    /// <summary>
    /// How to align this element based on its parent. You can adjust the position using the position field.
    /// </summary>
    public UIElementAlignment alignment;

    /// <summary>
    /// The position to move the element based on its alignment
    /// </summary>
    public Vector2Int position;

    /// <summary>
    /// The size of the element
    /// </summary>
    public Vector2Int size;

    /// <summary>
    /// Whether to adjust to the intrinsic size of the element
    /// </summary>
    public bool adjustToIntrinsicSize;

    /// <summary>
    /// The intrinsic size of the element. Some elements will have a size that fits all their content, which is calculated here, but not all.
    /// </summary>
    /// <returns></returns>
    public abstract Vector2Int IntrinsicSize();

    /// <summary>
    /// Renders the UI element
    /// </summary>
    /// <param name="position">The local screen position</param>
    /// <param name="viewID">The render view ID</param>
    public abstract void Render(Vector2Int position, ushort viewID);
}
