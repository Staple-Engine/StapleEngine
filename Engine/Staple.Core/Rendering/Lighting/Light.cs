namespace Staple;

/// <summary>
/// Light component
/// </summary>
public sealed class Light : Component
{
    /// <summary>
    /// Light type
    /// </summary>
    public LightType type = LightType.Directional;

    /// <summary>
    /// Light color
    /// </summary>
    public Color color = Color.White;

    /// <summary>
    /// Intensity of the light
    /// </summary>
    public float intensity = 1;

    /// <summary>
    /// Range if the type is <see cref="LightType.Point"/>
    /// </summary>
    public float range = 1;
}
