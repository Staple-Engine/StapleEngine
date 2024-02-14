using System.Numerics;

namespace Staple;

internal interface IAudioSource
{
    bool Playing { get; }

    bool Paused { get; }

    float Pitch { get; set; }

    float Volume { get; set; }

    Vector3 Position { get; set; }

    Vector3 Velocity { get; set; }

    bool Looping { get; set; }

    bool Init();

    void Destroy();

    bool Bind(IAudioClip clip);

    void Play();

    void Pause();

    void Stop();
}
