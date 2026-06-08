using System.Text.Json.Serialization;

namespace Staple;

[JsonConverter(typeof(JsonStringEnumConverter<RendererType>))]
public enum RendererType
{
    Direct3D12,
    Metal,
    Vulkan
}
