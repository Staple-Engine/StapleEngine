using System;
using System.Text.Json.Serialization;

namespace Staple;

/// <summary>
/// How to present the frame
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<VideoPresentationMode>))]
public enum VideoPresentationMode
{
    /// <summary>
    /// No waiting
    /// </summary>
    Immediate,

    /// <summary>
    /// Vertical Sync
    /// </summary>
    Vsync,

    /// <summary>
    /// Triple Buffering
    /// </summary>
    TripleBuffering,
}
