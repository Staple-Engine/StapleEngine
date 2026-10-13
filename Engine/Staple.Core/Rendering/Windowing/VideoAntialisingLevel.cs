using System;
using System.Text.Json.Serialization;

namespace Staple;

/// <summary>
/// How much anti aliasing to use
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<VideoAntialisingLevel>))]
public enum VideoAntialisingLevel
{
    /// <summary>
    /// No antialiasing
    /// </summary>
    None,

    /// <summary>
    /// MSAA (2x)
    /// </summary>
    MSAA2,

    /// <summary>
    /// MSAA (4x)
    /// </summary>
    MSAA4,

    /// <summary>
    /// MSAA (8x)
    /// </summary>
    MSAA8,
}