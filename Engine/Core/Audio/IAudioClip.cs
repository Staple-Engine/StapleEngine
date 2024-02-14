namespace Staple;

internal interface IAudioClip
{
    bool Init(short[] data, int channels, int bitsPerSample, int sampleRate);

    bool Init(byte[] data, int channels, int bitsPerSample, int sampleRate);

    void Destroy();
}
