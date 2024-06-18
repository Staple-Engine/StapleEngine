using System.Text.Json.Serialization;

namespace Staple;

/// <summary>
/// Type of platform Staple runs on
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AppPlatform>))]
public enum AppPlatform
{
    Windows,
    Linux,
    MacOSX,
    Android,
    iOS
}
