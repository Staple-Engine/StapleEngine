namespace Staple.Internal;

/// <summary>
/// Audio clip implementation interface
/// </summary>
public interface IAudioClip
{
    /// <summary>
    /// Initializes an audio clip through 16-bit samples.
    /// </summary>
    /// <param name="data">The audio clip data as an array of shorts</param>
    /// <param name="channels">The channels of the audio clip</param>
    /// <param name="bitsPerSample">The bits per sample for the audio clip</param>
    /// <param name="sampleRate">The sample rate of the audio clip</param>
    /// <returns>Whether it initialized successfully</returns>
    bool Init(short[] data, int channels, int bitsPerSample, int sampleRate);

    /// <summary>
    /// Initializes an audio clip through 8-bit samples.
    /// </summary>
    /// <param name="data">The audio clip data as an array of byte</param>
    /// <param name="channels">The channels of the audio clip</param>
    /// <param name="bitsPerSample">The bits per sample for the audio clip</param>
    /// <param name="sampleRate">The sample rate of the audio clip</param>
    /// <returns>Whether it initialized successfully</returns>
    bool Init(byte[] data, int channels, int bitsPerSample, int sampleRate);

    /// <summary>
    /// Destroys the audio clip instance
    /// </summary>
    void Destroy();
}
