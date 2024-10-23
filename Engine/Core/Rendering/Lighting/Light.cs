namespace Staple;

/// <summary>
/// Light component
/// </summary>
public sealed class Light : IComponent
{
    /// <summary>
    /// Light type
    /// </summary>
    public LightType type = LightType.Directional;

    /// <summary>
    /// Light color
    /// </summary>
    public Color color = Color.White;
}
