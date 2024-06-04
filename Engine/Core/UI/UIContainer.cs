namespace Staple.UI;

/// <summary>
/// UI Container component used to help layout UI
/// </summary>
public class UIContainer : UIElement
{
    public override Vector2Int IntrinsicSize()
    {
        return Vector2Int.Zero;
    }

    public override void Render(Vector2Int position, ushort viewID)
    {
        //Does nothing
    }
}
