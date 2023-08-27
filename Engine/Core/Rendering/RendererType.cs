using System.Text.Json.Serialization;

namespace Staple
{
    [JsonConverter(typeof(JsonStringEnumConverter<RendererType>))]
    public enum RendererType
    {
        OpenGLES,
        OpenGL,
        Direct3D11,
        Direct3D12,
        Metal,
        Vulkan
    }
}
