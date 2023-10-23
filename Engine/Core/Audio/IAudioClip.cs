namespace Staple
{
    internal interface IAudioClip
    {
        bool Init(byte[] data, int channels, int bitsPerSample, int sampleRate);

        void Destroy();
    }
}
