using System.Text.Json.Serialization;

namespace Staple
{
    [JsonConverter(typeof(JsonStringEnumConverter<WindowMode>))]
    public enum WindowMode
    {
        Windowed,
        Fullscreen,
        Borderless
    }
}
