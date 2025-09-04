namespace Staple.Internal;

/// <summary>
/// Audio device implementation interface
/// </summary>
public interface IAudioDevice
{
    /// <summary>
    /// Attempts to initialize the audio device
    /// </summary>
    /// <returns>Whether it was successfully initialized</returns>
    bool Init();

    /// <summary>
    /// Shuts down the audio device
    /// </summary>
    void Shutdown();
}
