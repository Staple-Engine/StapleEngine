using Staple.Internal;

namespace Staple;

/// <summary>
/// A source of audio. Plays audio clips.
/// </summary>
public sealed class AudioSource : IComponent
{
    /// <summary>
    /// The audio clip to use
    /// </summary>
    public AudioClip audioClip;

    /// <summary>
    /// The volume for the source
    /// </summary>
    public float volume = 1;

    /// <summary>
    /// The pitch for the source
    /// </summary>
    public float pitch = 1;

    /// <summary>
    /// Whether the audio loops
    /// </summary>
    public bool loop = false;

    /// <summary>
    /// Whether this is 3D audio
    /// </summary>
    public bool spatial = false;

    /// <summary>
    /// Whether to auto play
    /// </summary>
    public bool autoplay = false;

    /// <summary>
    /// The internal audio source instance
    /// </summary>
    internal IAudioSource audioSource;

    /// <summary>
    /// Whether we're playing audio
    /// </summary>
    public bool Playing => audioSource?.Playing ?? false;

    /// <summary>
    /// Whether we're paused
    /// </summary>
    public bool Paused => audioSource?.Paused ?? false;

    /// <summary>
    /// Plays the audio
    /// </summary>
    public void Play()
    {
        audioSource?.Play();
    }

    /// <summary>
    /// Pauses the audio
    /// </summary>
    public void Pause()
    {
        audioSource?.Pause();
    }

    /// <summary>
    /// Stops playing
    /// </summary>
    public void Stop()
    {
        audioSource?.Stop();
    }
}
