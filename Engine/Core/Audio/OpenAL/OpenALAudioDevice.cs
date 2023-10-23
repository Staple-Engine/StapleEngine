using OpenAL;

namespace Staple
{
    [AdditionalLibrary(AppPlatform.Android, "libopenal32.so")]
    internal class OpenALAudioDevice : IAudioDevice
    {
        public nint Device { get; private set; }

        public nint Context { get; private set; }

        public static bool CheckALError()
        {
            var error = AL10.alGetError();

            if(error != AL10.AL_NO_ERROR)
            {
                Log.Debug($"[AudioSystem] AL Error: {error.ToString("X")}");

                return true;
            }

            return false;
        }

        public bool Init()
        {
            Device = ALC10.alcOpenDevice(null);

            if(Device == nint.Zero)
            {
                Log.Debug("[AudioSystem] Failed to open device");

                return false;
            }

            Log.Debug("[AudioSystem] Device opened");

            Context = ALC10.alcCreateContext(Device, System.Array.Empty<int>());

            if (Context == nint.Zero)
            {
                Log.Debug("[AudioSystem] Failed to create context");

                ALC10.alcCloseDevice(Device);

                Device = nint.Zero;

                return false;
            }

            Log.Debug("[AudioSystem] Context created");

            if (ALC10.alcMakeContextCurrent(Context) == false)
            {
                Log.Debug("[AudioSystem] Failed to make context current");

                ALC10.alcDestroyContext(Context);
                ALC10.alcCloseDevice(Device);

                Device = nint.Zero;
                Context = nint.Zero;

                return false;
            }

            return true;
        }

        public void Shutdown()
        {
            ALC10.alcMakeContextCurrent(nint.Zero);

            if(Context != nint.Zero)
            {
                ALC10.alcDestroyContext(Context);

                Context = nint.Zero;
            }

            if(Device != nint.Zero)
            {
                ALC10.alcCloseDevice(Device);

                Device = nint.Zero;
            }
        }
    }
}
