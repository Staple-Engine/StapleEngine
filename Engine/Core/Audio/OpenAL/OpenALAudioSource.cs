using OpenAL;
using System.Numerics;

namespace Staple
{
    internal class OpenALAudioSource : IAudioSource
    {
        internal uint source;

        public float Pitch
        {
            get
            {
                if(source == 0)
                {
                    return 1;
                }

                AL10.alGetSourcef(source, AL10.AL_PITCH, out var value);

                return value;
            }

            set
            {
                if (source == 0)
                {
                    return;
                }

                AL10.alSourcef(source, AL10.AL_PITCH, value);
            }
        }

        public float Volume
        {
            get
            {
                if (source == 0)
                {
                    return 1;
                }

                AL10.alGetSourcef(source, AL10.AL_GAIN, out var value);

                return value;
            }

            set
            {
                if (source == 0)
                {
                    return;
                }

                AL10.alSourcef(source, AL10.AL_GAIN, value);
            }
        }

        public Vector3 Position
        {
            get
            {
                var value = Vector3.Zero;

                if (source == 0)
                {
                    return value;
                }

                AL10.alGetSource3f(source, AL10.AL_POSITION, out value.X, out value.Y, out value.Z);

                return value;
            }

            set
            {
                if (source == 0)
                {
                    return;
                }

                AL10.alSource3f(source, AL10.AL_POSITION, value.X, value.Y, value.Z);
            }
        }

        public Vector3 Velocity
        {
            get
            {
                var value = Vector3.Zero;

                if (source == 0)
                {
                    return value;
                }

                AL10.alGetSource3f(source, AL10.AL_VELOCITY, out value.X, out value.Y, out value.Z);

                return value;
            }

            set
            {
                if (source == 0)
                {
                    return;
                }

                AL10.alSource3f(source, AL10.AL_VELOCITY, value.X, value.Y, value.Z);
            }
        }

        public bool Looping
        {
            get
            {
                if (source == 0)
                {
                    return false;
                }

                AL10.alGetSourcei(source, AL10.AL_LOOPING, out var value);

                return value == AL10.AL_TRUE;
            }

            set
            {
                if (source == 0)
                {
                    return;
                }

                AL10.alSourcei(source, AL10.AL_LOOPING, value ? AL10.AL_TRUE : AL10.AL_FALSE);
            }
        }

        public bool Init()
        {
            AL10.alGenSources(1, out source);

            return source != 0;
        }

        public void Destroy()
        {
            if(source == 0)
            {
                return;
            }

            AL10.alDeleteSources(1, ref source);

            source = 0;
        }
    }
}
