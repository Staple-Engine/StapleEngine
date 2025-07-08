using System.Text.Json.Serialization;

namespace Staple;

[JsonConverter(typeof(JsonStringEnumConverter<RendererType>))]
public enum RendererType
{
    OpenGLES,
    OpenGL,
    Direct3D11,
#if STAPLE_SUPPORTS_D3D12
    Direct3D12,
#endif
    Metal,
    Vulkan
}
