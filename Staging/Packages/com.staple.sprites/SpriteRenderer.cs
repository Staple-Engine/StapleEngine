using System;

namespace Staple;

/// <summary>
/// Sprite Renderer component
/// </summary>
public sealed class SpriteRenderer : Renderable
{
    /// <summary>
    /// The sprite to use
    /// </summary>
    public Sprite sprite;

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
    /// How to render the sprite (<see cref="SpriteRenderMode"/>)
    /// </summary>
    public SpriteRenderMode renderMode = SpriteRenderMode.Normal;

    /// <summary>
    /// Whether to use the <see cref="animation"/>
    /// </summary>
    public bool useAnimation;

    /// <summary>
    /// Whether to loop the <see cref="animation"/>
    /// </summary>
    public bool loopAnimation;

    /// <summary>
    /// Called every time the <see cref="animation"/> finishes a loop
    /// </summary>
    public Action<SpriteRenderer> onAnimationFinished;

    /// <summary>
    /// Whether we're currently animating
    /// </summary>
    internal bool animating;

    /// <summary>
    /// The current animation frame
    /// </summary>
    internal int currentFrame;

    /// <summary>
    /// The animation timer
    /// </summary>
    internal float timer;

    /// <summary>
    /// Plays the attached <see cref="animation"/>
    /// </summary>
    public void PlayAnimation()
    {
        currentFrame = 0;
        timer = 0;
        animating = true;
        useAnimation = true;
    }

    /// <summary>
    /// Pauses the <see cref="animation"/>
    /// </summary>
    public void PauseAnimation()
    {
        animating = false;
    }

    /// <summary>
    /// Resumes playing the <see cref="animation"/>
    /// </summary>
    public void ResumeAnimation()
    {
        animating = true;
    }

    /// <summary>
    /// Stops the animation entirely. The sprite will no longer be replaced by the animated sprite.
    /// </summary>
    public void StopAnimation()
    {
        currentFrame = 0;
        timer = 0;
        animating = false;
        useAnimation = false;
    }
}
