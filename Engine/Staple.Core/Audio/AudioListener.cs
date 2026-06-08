using Staple.Internal;

namespace Staple;

/// <summary>
/// Audio listener component.
/// Used for 3D movement especially.
/// </summary>
public sealed class AudioListener : IComponent
{
    /// <summary>
    /// Whether this audio listener is spatial (3D)
    /// </summary>
    public bool spatial = false;

    /// <summary>
    /// Internal audio listener instance
    /// </summary>
    internal IAudioListener audioListener;
}
