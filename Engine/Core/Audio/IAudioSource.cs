using System.Numerics;

namespace Staple.Internal;

/// <summary>
/// Audio Source implementation interface
/// </summary>
public interface IAudioSource
{
    /// <summary>
    /// Whether we're playing
    /// </summary>
    bool Playing { get; }

    /// <summary>
    /// Whether we're paused
    /// </summary>
    bool Paused { get; }

    /// <summary>
    /// The audio pitch
    /// </summary>
    float Pitch { get; set; }

    /// <summary>
    /// The audio volume
    /// </summary>
    float Volume { get; set; }

    /// <summary>
    /// The position of this source
    /// </summary>
    Vector3 Position { get; set; }

    /// <summary>
    /// The velocity of this source
    /// </summary>
    Vector3 Velocity { get; set; }

    /// <summary>
    /// Whether we're looping
    /// </summary>
    bool Looping { get; set; }

    /// <summary>
    /// Attempts to initialize this audio source
    /// </summary>
    /// <returns>Whether it initialized successfully</returns>
    bool Init();

    /// <summary>
    /// Destroys this audio source
    /// </summary>
    void Destroy();

    /// <summary>
    /// Attempts to bind an audio clip to this source
    /// </summary>
    /// <param name="clip">The audio clip</param>
    /// <returns>Whether it was successfully bound</returns>
    bool Bind(IAudioClip clip);

    /// <summary>
    /// Plays the audio clip
    /// </summary>
    void Play();

    /// <summary>
    /// Pauses the audio clip
    /// </summary>
    void Pause();

    /// <summary>
    /// Stops the audio clip
    /// </summary>
    void Stop();
}
