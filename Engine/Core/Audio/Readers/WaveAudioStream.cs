using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Staple.Internal;

/// <summary>
/// Wave Audio Stream.
/// Reads WAV audio from a byte stream.
/// </summary>
internal class WaveAudioStream : IAudioStream, IDisposable
{
    private WaveFileReader reader = null;
    private ISampleProvider provider;

    private Stream stream;

    public int Channels => reader?.WaveFormat.Channels ?? 0;

    public int SampleRate => reader?.WaveFormat.SampleRate ?? 0;

    public int BitsPerSample => reader?.WaveFormat.BitsPerSample ?? 0;

    public TimeSpan TotalTime => reader?.TotalTime ?? default;

    public TimeSpan CurrentTime => default;

    private readonly object lockObject = new();

    public WaveAudioStream(Stream stream)
    {
        this.stream = stream;

        Open();
    }

    ~WaveAudioStream()
    {
        Close();
    }

    public void Open()
    {
        lock (lockObject)
        {
            reader?.Dispose();

            reader = new WaveFileReader(stream);

            provider = reader.ToSampleProvider();
        }
    }

    public void Close()
    {
        lock (lockObject)
        {
            stream?.Dispose();
            reader?.Dispose();

            reader = null;
            stream = null;
        }
    }

    public int Read(short[] buffer, int count)
    {
        lock (lockObject)
        {
            var samples = new float[count];

            count = provider.Read(samples, 0, count);

            for (var i = 0; i < count; i++)
            {
                var temp = (int)(32767f * samples[i]);

                if (temp > short.MaxValue)
                {
                    temp = short.MaxValue;
                }
                else if (temp < short.MinValue)
                {
                    temp = short.MinValue;
                }

                buffer[i] = (short)temp;
            }

            return count;
        }
    }

    public short[] ReadAll()
    {
        lock (lockObject)
        {
            if (reader == null || provider == null)
            {
                throw new InvalidOperationException("Stream has not been previously opened");
            }

            var samples = new float[44100];

            var outSamples = new List<float>();

            var count = 0;

            while ((count = provider.Read(samples, 0, samples.Length)) > 0)
            {
                outSamples.AddRange(samples.Take(count));
            }

            return outSamples.Select(x =>
                {
                    var temp = (int)(32767f * x);

                    if (temp > short.MaxValue)
                    {
                        temp = short.MaxValue;
                    }
                    else if (temp < short.MinValue)
                    {
                        temp = short.MinValue;
                    }

                    return (short)temp;
                })
                .ToArray();
        }
    }

    public void Dispose()
    {
        Close();

        GC.SuppressFinalize(this);
    }
}
