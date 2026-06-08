using System.Text.Json.Serialization;

namespace Staple;

/// <summary>
/// Mode for the game/app window. This applies only to desktop platforms.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<WindowMode>))]
public enum WindowMode
{
    /// <summary>
    /// Regular window
    /// </summary>
    Windowed,

    /// <summary>
    /// Exclusive fullscreen, will change resolution if needed
    /// </summary>
    ExclusiveFullscreen,

    /// <summary>
    /// Borderless fullscreen, will adjust to the desktop's size
    /// </summary>
    BorderlessFullscreen
}
