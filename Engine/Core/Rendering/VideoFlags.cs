using System;
using System.Text.Json.Serialization;

namespace Staple
{
    [JsonConverter(typeof(JsonStringEnumConverter<VideoFlags>))]
    [Flags]
    public enum VideoFlags
    {
        None = 0,
        Vsync = (1 << 1),
        MSAAX2 = (1 << 2),
        MSAAX4 = (1 << 3),
        MSAAX8 = (1 << 4),
        MSAAX16 = (1 << 5),
        HDR10 = (1 << 6),
        HiDPI = (1 << 7),
    }
}