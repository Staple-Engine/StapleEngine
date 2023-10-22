using OpenAL;
using System.Numerics;

namespace Staple
{
    internal class OpenALAudioListener : IAudioListener
    {
        private Quaternion rotation = Quaternion.Identity;

        public Vector3 Position
        {
            get
            {
                var value = Vector3.Zero;

                AL10.alGetListener3f(AL10.AL_POSITION, out value.X, out value.Y, out value.Z);

                return value;
            }

            set
            {
                AL10.alListener3f(AL10.AL_POSITION, value.X, value.Y, value.Z);
            }
        }

        public Vector3 Velocity
        {
            get
            {
                var value = Vector3.Zero;

                AL10.alGetListener3f(AL10.AL_VELOCITY, out value.X, out value.Y, out value.Z);

                return value;
            }

            set
            {
                AL10.alListener3f(AL10.AL_VELOCITY, value.X, value.Y, value.Z);
            }
        }

        public Quaternion Orientation
        {
            get
            {
                return rotation;
            }

            set
            {
                rotation = value;

                var rotationVector = Vector3.Normalize(Vector3.Transform(new Vector3(0, 0, -1), rotation));

                var values = new float[6]
                {
                    rotation.X,
                    rotation.Y,
                    rotation.Z,
                    0,
                    1,
                    0,
                };

                AL10.alListenerfv(AL10.AL_ORIENTATION, values);
            }
        }
    }
}
