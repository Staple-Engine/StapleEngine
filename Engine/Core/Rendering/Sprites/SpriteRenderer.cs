namespace Staple;

/// <summary>
/// Sprite Renderer component
/// </summary>
public sealed class SpriteRenderer : Renderable
{
    /// <summary>
    /// The renderer's material
    /// </summary>
    public Material material;

    /// <summary>
    /// The sprite texture to use
    /// </summary>
    public Texture texture;

    /// <summary>
    /// The sprite index to use
    /// </summary>
    public int spriteIndex = 0;

    /// <summary>
    /// The sprite's color
    /// </summary>
    public Color color = Color.White;

    /// <summary>
    /// Whether to flip the X axis
    /// </summary>
    public bool flipX = false;

    /// <summary>
    /// Whether to flip the Y axis
    /// </summary>
    public bool flipY = false;

    /// <summary>
    /// (Temp) Optional sprite animation
    /// </summary>
    public SpriteAnimation animation;

    /// <summary>
    /// The current animation frame
    /// </summary>
    internal int currentFrame;

    /// <summary>
    /// The animation timer
    /// </summary>
    internal float timer;
}
