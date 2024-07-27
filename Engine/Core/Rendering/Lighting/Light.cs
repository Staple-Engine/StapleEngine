namespace Staple;

public sealed class Light : IComponent
{
    public LightType type = LightType.Directional;
    public Color color = Color.White;
}
