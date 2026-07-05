using Staple.Internal;
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
        builder.AppendLine($"{RenderSystem.RenderStats.drawCalls} drawcalls ({RenderSystem.RenderStats.savedDrawCalls} saved, {RenderSystem.RenderStats.culledDrawCalls} culled)");
        builder.AppendLine($"{RenderSystem.RenderStats.triangleCount} triangles");
        builder.AppendLine($"{RenderSystem.RenderStats.instanceCount} instances");
        builder.AppendLine($"{RenderSystem.RenderStats.spatialNodeCount} spatial nodes");
        builder.AppendLine($"{RenderSystem.RenderStats.spatialPartitionSize} spatial partition size");

        Text = builder.ToString();
    }
}
