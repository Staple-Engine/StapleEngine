using System.Numerics;

namespace Staple
{
    internal interface IAudioSource
    {
        float Pitch { get; set; }

        float Volume { get; set; }

        Vector3 Position { get; set; }

        Vector3 Velocity { get; set; }

        bool Looping { get; set; }

        bool Init();

        void Destroy();
    }
}
