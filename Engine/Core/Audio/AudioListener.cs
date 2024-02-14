namespace Staple;

public sealed class AudioListener : IComponent
{
    public bool spatial = false;

    internal IAudioListener audioListener;
}
