using Staple;
using Staple.UI;

public class FPSCounterComponent : CallbackComponent
{
	private SceneQuery<UIText, FPSCounterComponent> counters = new();

	public override void FixedUpdate()
	{
		var fps = $"{Time.FPS}";

        foreach (var (_, text, _) in counters)
		{
			text.text = fps;
		}
	}
}
