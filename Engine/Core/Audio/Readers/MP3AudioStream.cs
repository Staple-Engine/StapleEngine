using NLayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Staple.Internal
{
    internal class MP3AudioStream : IAudioStream, IDisposable
    {
        private MpegFile reader = null;

        private Stream stream;

        public int Channels => reader?.Channels ?? 0;

        public int SampleRate => reader?.SampleRate ?? 0;

        public int BitsPerSample => 16;

        public TimeSpan TotalTime => reader?.Duration ?? default;

        public TimeSpan CurrentTime => reader?.Time ?? default;

        private object lockObject = new();

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
            lock (lockObject)
            {
                reader?.Dispose();

                reader = new MpegFile(stream);
            }
        }

        public void Close()
        {
            lock(lockObject)
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
                    return default;
                }

                var samples = new float[reader.Length / sizeof(float)];

                if(reader.ReadSamples(samples, 0, samples.Length) != samples.Length)
                {
                    reader.Dispose();
                    reader = null;

                    return default;
                }

                return samples.Select(x =>
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
