using System;

namespace Staple;

/// <summary>
/// Time information class
/// </summary>
public static class Time
{
    /// <summary>
    /// The scale to apply to time. For example, a value of 2 would speed things up by 100%, while a value of 0.5 would slow things down by 50%
    /// </summary>
    public static float timeScale = 1.0f;

    /// <summary>
    /// The current time in seconds since booting the app, scaled by the time scale
    /// </summary>
    public static float time => unscaledTime * timeScale;

    /// <summary>
    /// The current time in seconds since booting the app, unscaled
    /// </summary>
    public static float unscaledTime { get; internal set; }

    /// <summary>
    /// The maximum value delta time might reach
    /// </summary>
    public static float maximumDeltaTime = 1.0f;

    /// <summary>
    /// The last frame's delta time, scaled by the time scale
    /// </summary>
    public static float deltaTime  => unscaledDeltaTime * timeScale;

    /// <summary>
    /// The last frame's delta time, unscaled
    /// </summary>
    public static float unscaledDeltaTime { get; internal set; }

    /// <summary>
    /// The fixed tick delta time, unscaled
    /// </summary>
    public static float fixedDeltaTime { get; internal set; }

    /// <summary>
    /// The current frame rate
    /// </summary>
    public static int FPS { get; internal set; }

    /// <summary>
    /// The current time accumulator
    /// </summary>
    internal static float Accumulator { get; private set; }

    /// <summary>
    /// Called when the accumulator triggers
    /// </summary>
    internal static Action OnAccumulatorFinished { get; set; }

    private static int frames;
    private static float frameTimer;

    /// <summary>
    /// Updates the clock
    /// </summary>
    /// <param name="current">The current time</param>
    /// <param name="last">The previous time</param>
    internal static void UpdateClock(DateTime current, DateTime last)
    {
        var delta = (float)(current - last).TotalSeconds;

        unscaledTime += delta;

        if (delta > maximumDeltaTime)
        {
            delta = maximumDeltaTime;
        }

        Accumulator += delta;

        unscaledDeltaTime = delta;

        var previousAccumulator = Accumulator;

        if(fixedDeltaTime > 0)
        {
            while (Accumulator >= fixedDeltaTime)
            {
                Accumulator -= fixedDeltaTime;
            }
        }

        if(Accumulator < previousAccumulator)
        {
            OnAccumulatorFinished?.Invoke();
        }

        frames++;
        frameTimer += delta;

        if(frameTimer >= 1.0f)
        {
            FPS = frames;

            frameTimer = 0;
            frames = 0;
        }
    }
}
