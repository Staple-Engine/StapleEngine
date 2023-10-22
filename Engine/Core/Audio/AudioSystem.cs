
namespace Staple.Internal
{
    internal class AudioSystem : ISubsystem
    {
        public SubsystemType type => SubsystemType.Update;

        internal IAudioDevice device;

        internal static readonly byte Priority = 3;

        public static readonly AudioSystem Instance = new();

        public void Startup()
        {
            device = new OpenALAudioDevice();

            if(device.Init() == false)
            {
                device = null;
            }

            Log.Debug(device != null ? "[AudioSystem] Initialized audio device" : "[AudioSystem] Failed to initialize audio device");
        }

        public void Update()
        {
        }

        public void Shutdown()
        {
            device?.Shutdown();
        }
    }
}
