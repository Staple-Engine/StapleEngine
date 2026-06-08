using System.Numerics;

namespace Staple.Internal;

/// <summary>
/// Audio listener implementation interface
/// </summary>
public interface IAudioListener
{
    /// <summary>
    /// The current position of the audio listener
    /// </summary>
    Vector3 Position { get; set; }

    /// <summary>
    /// The current velocity of the audio listener
    /// </summary>
    Vector3 Velocity { get; set; }

    /// <summary>
    /// The current orientation of the audio listener
    /// </summary>
    Quaternion Orientation { get; set; }
}
