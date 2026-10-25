using Staple.Internal;

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

    /// <summary>
    /// Whether this light is transitioning in some way
    /// </summary>
    internal CullingState transition = CullingState.None;

    /// <summary>
    /// The interpolation of the transition
    /// </summary>
    internal float transitionTimer;

    /// <summary>
    /// The current state regarding whether this was culled or not
    /// </summary>
    internal CullingState culling = CullingState.None;
}
