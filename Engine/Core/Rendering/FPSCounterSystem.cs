using Staple.UI;

namespace Staple.Internal;

public class FPSCounterSystem : IEntitySystemFixedUpdate
{
    private readonly SceneQuery<UIText, FPSCounter> counters = new();

    public void FixedUpdate(float deltaTime)
    {
        if(counters.Length == 0)
        {
            return;
        }

        foreach (var (_, text, counter) in counters.Contents)
        {
            text.text = string.Format(counter.format, Time.FPS);
        }
    }
}
