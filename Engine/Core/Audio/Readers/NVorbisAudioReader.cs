using NVorbis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Staple.Internal
{
    internal static class NVorbisAudioReader
    {
        public static bool ReadOGG(byte[] data, out short[] samples, out int channels, out int sampleRate)
        {
            samples = Array.Empty<short>();
            channels = 0;
            sampleRate = 0;

            using var stream = new MemoryStream(data);

            try
            {
                using var vorbis = new VorbisReader(stream);

                channels = vorbis.Channels;
                sampleRate = vorbis.SampleRate;

                var buffer = new float[1024];

                var samplesBuffer = new List<float>();

                var count = 0;

                while((count = vorbis.ReadSamples(buffer, 0, buffer.Length)) > 0)
                {
                    if(count < buffer.Length)
                    {
                        samplesBuffer.AddRange(buffer.Take(count));

                        break;
                    }
                    else
                    {
                        samplesBuffer.AddRange(buffer);
                    }
                }

                samples = samplesBuffer.Select(x =>
                    {
                        var temp = (int)(32767f * x);

                        if(temp > short.MaxValue)
                        {
                            temp = short.MaxValue;
                        }
                        else if(temp < short.MinValue)
                        {
                            temp = short.MinValue;
                        }

                        return (short)temp;
                    })
                    .ToArray();

                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }
    }
}
