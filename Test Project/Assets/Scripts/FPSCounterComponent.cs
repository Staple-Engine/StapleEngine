using Staple;
using Staple.UI;

namespace TestGame;

public class FPSCounterComponent : CallbackComponent
{
	private readonly SceneQuery<UIText, FPSCounterComponent> counters = new();

	public override void FixedUpdate()
	{
		var fps = $"{Time.FPS}";

        foreach (var (_, text, _) in counters.Contents)
		{
			text.text = fps;
		}
	}
}
