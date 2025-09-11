using Staple.Internal;
using System;
using System.Text;

namespace Staple.UI;

public class UIRenderStats(UIManager manager, string ID) : UIText(manager, ID)
{
    private readonly StringBuilder builder = new();

    public override void Update(Vector2Int parentPosition)
    {
        base.Update(parentPosition);

        builder.Clear();

        builder.AppendLine($"{World.Current.EntityCount} Entities");
        builder.AppendLine($"{RenderSystem.RenderStats.drawCalls} drawcalls ({RenderSystem.RenderStats.batchedDrawCalls} batched, {RenderSystem.RenderStats.triangleCount} triangles)");
        builder.AppendLine($"{RenderSystem.RenderStats.culledDrawCalls} culled renderers");

        Text = builder.ToString();
    }
}
