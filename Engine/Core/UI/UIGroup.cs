namespace Staple.UI;

public class UIGroup(UIManager manager) : UIPanel(manager)
{
    public override void SetSkin(UISkin skin)
    {
    }

    public override void Update(Vector2Int parentPosition)
    {
        PerformLayout();

        foreach(var child in Children)
        {
            child.Update(parentPosition + Position);
        }
    }

    public override void Draw(Vector2Int parentPosition)
    {
        var position = parentPosition + Position;

        if (IsCulled(position))
        {
            return;
        }

        foreach (var child in Children)
        {
            if(child.Visible)
            {
                child.Draw(position);
            }
        }
    }
}
