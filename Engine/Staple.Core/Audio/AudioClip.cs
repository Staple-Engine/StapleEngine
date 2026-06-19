using Staple.Internal;
using System;
using System.IO;

namespace Staple;

/// <summary>
/// Audio clip asset.
/// Contains data on an audio file.
/// </summary>
public sealed class AudioClip : IGuidAsset
{
    /// <summary>
    /// Audio metadata
    /// </summary>
    internal AudioClipMetadata metadata => audioResource?.metadata;

    /// <summary>
    /// Size in bytes of the contained file
    /// </summary>
    internal int sizeInBytes => audioResource?.sizeInBytes ?? 0;

    /// <summary>
    /// Duration in seconds
    /// </summary>
    internal float duration => audioResource?.duration ?? 0;

    /// <summary>
    /// Audio channels
    /// </summary>
    internal int channels => audioResource?.channels ?? 0;

    /// <summary>
    /// Bits per sample
    /// </summary>
    internal int bitsPerSample => audioResource?.bitsPerSample ?? 0;

    /// <summary>
    /// Sample rate
    /// </summary>
    internal int sampleRate => audioResource?.sampleRate ?? 0;

    /// <summary>
    /// 16-bit samples
    /// </summary>
    internal short[] samples => audioResource?.samples;

    /// <summary>
    /// What kind of audio format we have
    /// </summary>
    internal AudioClipFormat format => audioResource?.format ?? AudioClipFormat.WAV;

    /// <summary>
    /// The file's contents
    /// </summary>
    internal byte[] fileData => audioResource?.fileData;

    internal AudioClipResource audioResource;

    public GuidHasher Guid => audioResource?.Guid ?? new();

    /// <summary>
    /// Gets an internal audio stream for the audio.
    /// </summary>
    /// <returns>The audio stream, or null</returns>
    internal IAudioStream GetAudioStream()
    {
        if(audioResource == null)
        {
            return null;
        }

        switch(format)
        {
            case AudioClipFormat.MP3:

                try
                {
                    var stream = new MemoryStream(fileData);

                    try
                    {
                        return new MP3AudioStream(stream);
                    }
                    catch (Exception e)
                    {
                        stream.Dispose();

                        Log.Error($"[AudioSystem] Failed to load audio clip for {Guid.Guid}: {e}");
                    }
                }
                catch (Exception)
                {
                }

                break;

            case AudioClipFormat.OGG:

                try
                {
                    var stream = new MemoryStream(fileData);

                    try
                    {
                        return new OggAudioStream(stream);
                    }
                    catch (Exception e)
                    {
                        stream.Dispose();

                        Log.Error($"[AudioSystem] Failed to load audio clip for {Guid.Guid}: {e}");
                    }
                }
                catch(Exception)
                {
                }

                break;

            case AudioClipFormat.WAV:

                try
                {
                    var stream = new MemoryStream(fileData);

                    try
                    {
                        return new WaveAudioStream(stream);
                    }
                    catch (Exception e)
                    {
                        stream.Dispose();

                        Log.Error($"[AudioSystem] Failed to load audio clip for {Guid.Guid}: {e}");
                    }
                }
                catch(Exception)
                {
                }

                break;
        }

        return null;
    }

    public static object Create(string path) => ResourceManager.instance.LoadAudioClip(path);
}
