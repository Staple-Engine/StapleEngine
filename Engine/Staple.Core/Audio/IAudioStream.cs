using System;

namespace Staple.Internal;

/// <summary>
/// Audio Stream implementation interface
/// </summary>
internal interface IAudioStream
{
    /// <summary>
    /// How many channels the audio has
    /// </summary>
    int Channels { get; }

    /// <summary>
    /// The sample rate of the audio
    /// </summary>
    int SampleRate { get; }

    /// <summary>
    /// The bits per sample of the audio
    /// </summary>
    int BitsPerSample { get; }

    /// <summary>
    /// The total duration of the audio
    /// </summary>
    TimeSpan TotalTime { get; }

    /// <summary>
    /// The current time we're at in the audio
    /// </summary>
    TimeSpan CurrentTime { get; }

    /// <summary>
    /// Attempts to read count samples from the audio stream
    /// </summary>
    /// <param name="buffer">A buffer to keep the data</param>
    /// <param name="count">How many samples to read</param>
    /// <returns>How many samples were read</returns>
    int Read(short[] buffer, int count);

    /// <summary>
    /// Attempts to read all the audio samples from the audio stream
    /// </summary>
    /// <returns>The samples, or empty</returns>
    short[] ReadAll();

    /// <summary>
    /// Opens the audio stream for usage
    /// </summary>
    void Open();

    /// <summary>
    /// Closes the audio stream after using it
    /// </summary>
    void Close();
}
