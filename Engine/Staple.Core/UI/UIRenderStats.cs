using Staple.Internal;
using System;

namespace Staple.UI;

public class UIRenderStats(UIManager manager, string ID) : UIText(manager, ID)
{
    public override void Update(Vector2Int parentPosition)
    {
        base.Update(parentPosition);

        try
        {
            Text = $"{World.Current.EntityCount} Entities\n{RenderSystem.DrawnRenderers} rendered ({RenderSystem.CulledRenderers} culled)";
        }
        catch (Exception)
        {
        }
    }
}
