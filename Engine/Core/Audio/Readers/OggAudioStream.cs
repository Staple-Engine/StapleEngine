using NVorbis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Staple.Internal
{
    internal class OggAudioStream : IAudioStream, IDisposable
    {
        private VorbisReader vorbis = null;

        private Stream stream;

        public int Channels => vorbis?.Channels ?? 0;

        public int SampleRate => vorbis?.SampleRate ?? 0;

        public int BitsPerSample => 16;

        public TimeSpan TotalTime => vorbis?.TotalTime ?? default;

        public TimeSpan CurrentTime => vorbis?.TimePosition ?? default;

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
            vorbis?.Dispose();

            vorbis = new VorbisReader(stream);
        }

        public void Close()
        {
            vorbis?.Dispose();

            vorbis = null;
        }

        public int Read(short[] buffer, int count)
        {
            var samples = new float[count];

            count = vorbis.ReadSamples(samples, 0, count);

            for(var i = 0; i < count; i++)
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

        public short[] ReadAll()
        {
            if(vorbis == null)
            {
                throw new InvalidOperationException("Stream has not been previously opened");
            }

            var samples = new float[44100];

            var outSamples = new List<float>();

            var count = 0;

            while((count = vorbis.ReadSamples(samples, 0, samples.Length)) > 0)
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

        public void Dispose()
        {
            Close();
        }
    }
}
