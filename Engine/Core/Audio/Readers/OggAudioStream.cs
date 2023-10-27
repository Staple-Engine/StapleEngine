using NVorbis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Staple.Internal
{
    internal class OggAudioStream : IAudioStream, IDisposable
    {
        private VorbisReader reader = null;

        private Stream stream;

        public int Channels => reader?.Channels ?? 0;

        public int SampleRate => reader?.SampleRate ?? 0;

        public int BitsPerSample => 16;

        public TimeSpan TotalTime => reader?.TotalTime ?? default;

        public TimeSpan CurrentTime => reader?.TimePosition ?? default;

        private object lockObject = new();

        public OggAudioStream(Stream stream)
        {
            this.stream = stream;

            Open();
        }

        ~OggAudioStream()
        {
            Close();
        }

        public void Open()
        {
            lock (lockObject)
            {
                reader?.Dispose();

                reader = new VorbisReader(stream);
            }
        }

        public void Close()
        {
            lock (lockObject)
            {
                reader?.Dispose();

                reader = null;
            }
        }

        public int Read(short[] buffer, int count)
        {
            lock (lockObject)
            {
                var samples = new float[count];

                count = reader.ReadSamples(samples, 0, count);

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
                if (reader == null)
                {
                    throw new InvalidOperationException("Stream has not been previously opened");
                }

                var samples = new float[44100];

                var outSamples = new List<float>();

                var count = 0;

                while ((count = reader.ReadSamples(samples, 0, samples.Length)) > 0)
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
}
