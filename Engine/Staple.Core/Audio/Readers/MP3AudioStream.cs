using DrLibs;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Staple.Internal;

/// <summary>
/// MP3 Audio Stream.
/// Reads MP3 audio from a byte stream.
/// </summary>
internal class MP3AudioStream : IAudioStream, IDisposable
{
    private Stream stream;
    private short[] samples;

    public int Channels { get; private set; }

    public int SampleRate { get; private set; }

    public int BitsPerSample { get; private set; }

    public TimeSpan TotalTime { get; private set; }

    public TimeSpan CurrentTime { get; private set; }

    private readonly object lockObject = new();

    public MP3AudioStream(Stream stream)
    {
        this.stream = stream;

        Open();
    }

    ~MP3AudioStream()
    {
        Close();
    }

    public void Open()
    {
    }

    public void Close()
    {
        lock(lockObject)
        {
            stream?.Dispose();
            stream = null;
        }
    }

    private void Load()
    {
        if(stream is MemoryStream memory)
        {
            var data = memory.ToArray();

            stream.Dispose();

            stream = null;

            int channels;
            int bitsPerChannel;
            int sampleRate;
            float duration;
            int requiredSize;

            unsafe
            {
                fixed(byte *b = data)
                {
                    var ptr = DrMp3.LoadMP3(b, data.Length, &channels, &bitsPerChannel, &sampleRate, &duration, &requiredSize);

                    if(ptr == nint.Zero)
                    {
                        return;
                    }

                    var buffer = DrMp3.GetMP3Buffer(ptr);

                    if(buffer == nint.Zero)
                    {
                        return;
                    }

                    samples = new short[requiredSize / sizeof(ushort)];

                    Marshal.Copy(buffer, samples, 0, samples.Length);

                    DrMp3.FreeMP3(ptr);

                    Channels = channels;
                    BitsPerSample = bitsPerChannel;
                    SampleRate = sampleRate;
                    TotalTime = TimeSpan.FromSeconds(duration);
                }
            }
        }
    }

    public int Read(short[] buffer, int count)
    {
        lock (lockObject)
        {
            if(samples == default)
            {
                Load();
            }

            Buffer.BlockCopy(samples, 0, buffer, 0, count);

            return count;
        }
    }

    public short[] ReadAll()
    {
        lock (lockObject)
        {
            if (samples == default)
            {
                Load();
            }

            return samples;
        }
    }

    public void Dispose()
    {
        Close();

        GC.SuppressFinalize(this);
    }
}
