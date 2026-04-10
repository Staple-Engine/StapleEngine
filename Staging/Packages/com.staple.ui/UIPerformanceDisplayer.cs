using Staple.Internal;
using System.Linq;
using System.Text;

namespace Staple.UI;

public class UIPerformanceDisplayer(UIManager manager, string ID) : UIText(manager, ID)
{
    private readonly StringBuilder builder = new();

    public override void Update(Vector2Int parentPosition)
    {
        base.Update(parentPosition);

        builder.Clear();

        switch(AppSettings.Current?.profilingMode ?? AppProfilingMode.None)
        {
            case AppProfilingMode.RenderStats:

                builder.AppendLine($"{World.Current.EntityCount} Entities");
                builder.AppendLine($"{RenderSystem.RenderStats.drawCalls} drawcalls ({RenderSystem.RenderStats.savedDrawCalls} saved, {RenderSystem.RenderStats.culledDrawCalls} culled)");
                builder.AppendLine($"{RenderSystem.RenderStats.triangleCount} triangles");

                break;

            case AppProfilingMode.Profiler:

                {
                    var counters = PerformanceProfilerSystem.AverageFrameCounters
                        .OrderByDescending(x => x.Value)
                        .ToArray();

                    for (var i = 0; i < counters.Length; i++)
                    {
                        var counter = counters[i];

                        builder.AppendLine($"{counter.Key} - {counter.Value}ms");
                    }
                }

                break;
        }

        Text = builder.ToString();
    }
}
