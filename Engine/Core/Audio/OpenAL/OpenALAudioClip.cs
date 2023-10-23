using OpenAL;

namespace Staple
{
    internal class OpenALAudioClip : IAudioClip
    {
        public uint buffer;

        public bool Init(byte[] data, int channels, int bitsPerSample, int sampleRate)
        {
            var stereo = channels > 1;

            var format = bitsPerSample switch
            {
                16 => stereo ? AL10.AL_FORMAT_STEREO16 : AL10.AL_FORMAT_MONO16,
                8 => stereo ? AL10.AL_FORMAT_STEREO8 : AL10.AL_FORMAT_MONO8,
                _ => 0,
            };

            if(format == 0)
            {
                return false;
            }

            AL10.alGenBuffers(1, out buffer);

            if(OpenALAudioDevice.CheckALError())
            {
                buffer = 0;

                return false;
            }

            AL10.alBufferData(buffer, format, data, data.Length, sampleRate);

            if(OpenALAudioDevice.CheckALError())
            {
                AL10.alDeleteBuffers(1, ref buffer);

                buffer = 0;

                return false;
            }

            return true;
        }

        public void Destroy()
        {
            if(buffer > 0)
            {
                AL10.alDeleteBuffers(1, ref buffer);

                buffer = 0;
            }
        }
    }
}
