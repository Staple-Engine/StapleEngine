using System.Text.Json.Serialization;

namespace Staple
{
    [JsonConverter(typeof(JsonStringEnumConverter<AppPlatform>))]
    public enum AppPlatform
    {
        Windows,
        Linux,
        MacOSX,
    }
}
