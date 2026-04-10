using System.Collections.Generic;

namespace Staple.UI;

public class UIGroup(UIManager manager, string ID) : UIPanel(manager, ID)
{
    public override void SetSkin(UISkin skin)
    {
    }

    public override void ApplyLayoutProperties(Dictionary<string, object> properties)
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
