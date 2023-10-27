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

                var samples = new byte[reader.Length];

                var read = reader.ReadSamples(samples, 0, samples.Length);

                while(read != samples.Length)
                {
                    var diff = samples.Length - read;

                    var count = reader.ReadSamples(samples, read, diff);

                    read += count;
                }

                var outData = new short[samples.Length / sizeof(short)];

                Buffer.BlockCopy(samples, 0, outData, 0, samples.Length);

                return outData;
            }
        }

        public void Dispose()
        {
            Close();

            GC.SuppressFinalize(this);
        }
    }
}
