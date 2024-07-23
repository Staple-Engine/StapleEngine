namespace Staple;

public sealed class Light : IComponent
{
    public LightType type = LightType.Directional;
    public Color color = Color.White;
    public LayerMask cullingMask = LayerMask.Everything;

    [Range(0, 1)]
    public float intensity = 1;

    public float range = 1;
    public LayerMask renderingLayerMask = LayerMask.Everything;
}
