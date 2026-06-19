namespace Staple.Internal;

internal class AudioClipResource
{
    /// <summary>
    /// Audio metadata
    /// </summary>
    internal AudioClipMetadata metadata;

    /// <summary>
    /// Size in bytes of the contained file
    /// </summary>
    internal int sizeInBytes;

    /// <summary>
    /// Duration in seconds
    /// </summary>
    internal float duration;

    /// <summary>
    /// Audio channels
    /// </summary>
    internal int channels;

    /// <summary>
    /// Bits per sample
    /// </summary>
    internal int bitsPerSample;

    /// <summary>
    /// Sample rate
    /// </summary>
    internal int sampleRate;

    /// <summary>
    /// 16-bit samples
    /// </summary>
    internal short[] samples;

    /// <summary>
    /// What kind of audio format we have
    /// </summary>
    internal AudioClipFormat format;

    /// <summary>
    /// The file's contents
    /// </summary>
    internal byte[] fileData;

    public GuidHasher Guid = new();
}
