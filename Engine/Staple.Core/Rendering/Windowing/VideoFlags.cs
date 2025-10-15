using System;
using System.Text.Json.Serialization;

namespace Staple;

[JsonConverter(typeof(JsonStringEnumConverter<VideoFlags>))]
[Flags]
public enum VideoFlags
{
    None = 0,
    Vsync = (1 << 1),
    TripleBuffering = (1 << 2),
    HDR10 = (1 << 6),
}