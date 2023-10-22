
namespace Staple
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

            if(device == null)
            {
                return;
            }

            World.AddComponentAddedCallback(typeof(AudioListener), (Entity entity, Transform transform, ref IComponent component) =>
            {
                var listener = component as AudioListener;

                listener.audioListener = new OpenALAudioListener
                {
                    Position = transform.Position,
                    Orientation = transform.Rotation
                };
            });

            World.AddComponentAddedCallback(typeof(AudioSource), (Entity entity, Transform transform, ref IComponent component) =>
            {
                var source = component as AudioSource;

                source.audioSource = new OpenALAudioSource();

                if(source.audioSource.Init() == false)
                {
                    Log.Debug($"[AudioSystem] Failed to create audio source for entity {Scene.current.world.GetEntityName(entity)}");

                    source.audioSource = null;
                }
            });

            World.AddComponentRemovedCallback(typeof(AudioListener), (Entity entity, Transform transform, ref IComponent component) =>
            {
                var listener = component as AudioListener;

                listener.audioListener = null;
            });

            World.AddComponentRemovedCallback(typeof(AudioSource), (Entity entity, Transform transform, ref IComponent component) =>
            {
                var source = component as AudioSource;

                source.audioSource?.Destroy();

                source.audioSource = null;
            });
        }

        public void Update()
        {
            Scene.current?.world?.ForEach((Entity entity, bool enabled, ref Transform transform, ref AudioListener listener) =>
            {
                listener.audioListener.Position = transform.Position;
                listener.audioListener.Orientation = transform.Rotation;
            });
        }

        public void Shutdown()
        {
            device?.Shutdown();
        }
    }
}
