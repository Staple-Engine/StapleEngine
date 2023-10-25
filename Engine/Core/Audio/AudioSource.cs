namespace Staple
{
    public sealed class AudioSource : IComponent
    {
        public AudioClip audioClip;
        public float volume = 1;
        public float pitch = 1;
        public bool loop = false;
        public bool spatial = false;

        internal IAudioSource audioSource;

        public bool Playing => audioSource?.Playing ?? false;

        public bool Paused => audioSource?.Paused ?? false;

        public void Play()
        {
            audioSource?.Play();
        }

        public void Pause()
        {
            audioSource?.Pause();
        }

        public void Stop()
        {
            audioSource?.Stop();
        }
    }
}
