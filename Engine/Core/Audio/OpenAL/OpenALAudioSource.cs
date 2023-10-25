using OpenAL;
using System.Numerics;

namespace Staple
{
    internal class OpenALAudioSource : IAudioSource
    {
        internal uint source;

        public bool Playing
        {
            get
            {
                if (source == 0)
                {
                    return false;
                }

                AL10.alGetSourcei(source, AL10.AL_SOURCE_STATE, out var value);

                return value == AL10.AL_PLAYING;
            }
        }

        public bool Paused
        {
            get
            {
                if (source == 0)
                {
                    return false;
                }

                AL10.alGetSourcei(source, AL10.AL_SOURCE_STATE, out var value);

                return value == AL10.AL_PAUSED;
            }
        }

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

        public bool Bind(IAudioClip clip)
        {
            if(clip is not OpenALAudioClip audioClip ||
                audioClip.buffer == 0 ||
                source == 0)
            {
                return false;
            }

            if(audioClip.buffer != 0)
            {
                AL10.alSourcei(source, AL10.AL_BUFFER, (int)audioClip.buffer);

                if (OpenALAudioDevice.CheckALError("AudioSource Bind"))
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        public void Play()
        {
            if(source == 0)
            {
                return;
            }

            if(Paused == false)
            {
                AL10.alSourceRewind(source);
            }

            AL10.alSourcePlay(source);

            OpenALAudioDevice.CheckALError("AudioSource Play");
        }

        public void Pause()
        {
            if(source == 0)
            {
                return;
            }

            AL10.alSourcePause(source);

            OpenALAudioDevice.CheckALError("AudioSource Pause");
        }

        public void Stop()
        {
            if (source == 0)
            {
                return;
            }

            AL10.alSourceStop(source);

            OpenALAudioDevice.CheckALError("AudioSource Stop");
        }
    }
}
