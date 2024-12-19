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

    private string guid;

    private int guidHash;

    public int GuidHash => guidHash;

    public string Guid
    {
        get => guid;
        
        set
        {
            guid = value;

            guidHash = guid?.GetHashCode() ?? 0;
        }
    }

    /// <summary>
    /// Gets an internal audio stream for the audio.
    /// </summary>
    /// <returns>The audio stream, or null</returns>
    internal IAudioStream GetAudioStream()
    {
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

                        Log.Error($"[AudioSystem] Failed to load audio clip for {guid}: {e}");
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

                        Log.Error($"[AudioSystem] Failed to load audio clip for {guid}: {e}");
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

                        Log.Error($"[AudioSystem] Failed to load audio clip for {guid}: {e}");
                    }
                }
                catch(Exception)
                {
                }

                break;
        }

        return null;
    }

    public static object Create(string path)
    {
        return ResourceManager.instance.LoadAudioClip(path);
    }
}
