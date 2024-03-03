using Staple.Internal;
using System;
using System.IO;

namespace Staple;

public sealed class AudioClip : IGuidAsset
{
    public AudioClipMetadata metadata;

    [NonSerialized]
    public int sizeInBytes;

    [NonSerialized]
    public float duration;

    [NonSerialized]
    public int channels;

    [NonSerialized]
    public int bitsPerSample;

    [NonSerialized]
    public int sampleRate;

    internal short[] samples;

    internal AudioClipFormat format;

    internal byte[] fileData;

    private string guid;

    public string Guid { get => guid; set => guid = value; }

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
