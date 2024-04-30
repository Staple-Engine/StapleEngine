using System.Numerics;

namespace Staple.Internal;

public interface IAudioListener
{
    Vector3 Position { get; set; }

    Vector3 Velocity { get; set; }

    Quaternion Orientation { get; set; }
}
