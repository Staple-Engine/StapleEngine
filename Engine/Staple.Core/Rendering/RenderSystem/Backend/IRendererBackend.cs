using System;
using System.Numerics;

namespace Staple.Internal;

internal interface IRendererBackend
{
    bool SupportsTripleBuffering { get; }

    bool SupportsHDRColorSpace { get; }

    bool SupportsLinearColorSpace { get; }

    bool Initialize(RendererType renderer, bool debug, IRenderWindow window, RenderModeFlags renderFlags);

    void Destroy();

    void BeginFrame();

    void EndFrame();

    void UpdateRenderMode(RenderModeFlags flags);

    void UpdateViewport(int width, int height);

    IRenderPass BeginRenderPass(RenderTarget target, CameraClearMode clear, Color clearColor, Vector4 viewport,
        Matrix4x4 view, Matrix4x4 projection);

    VertexBuffer CreateVertexBuffer(Span<byte> data, VertexLayout layout, RenderBufferFlags flags);

    VertexBuffer CreateVertexBuffer<T>(Span<T> data, VertexLayout layout, RenderBufferFlags flags) where T : unmanaged;

    IndexBuffer CreateIndexBuffer(Span<ushort> data, RenderBufferFlags flags);

    IndexBuffer CreateIndexBuffer(Span<uint> data, RenderBufferFlags flags);

    VertexLayoutBuilder CreateVertexLayoutBuilder();

    IShaderProgram CreateShaderVertexFragment(byte[] vertex, byte[] fragment,
        VertexFragmentShaderMetrics vertexMetrics, VertexFragmentShaderMetrics fragmentMetrics,
        VertexAttribute[] vertexAttributes);

    IShaderProgram CreateShaderCompute(byte[] compute, ComputeShaderMetrics computeMetrics);

    bool SupportsTextureFormat(TextureFormat format, TextureFlags flags);

    ITexture CreateTextureAssetTexture(SerializableTexture asset, TextureFlags flags);

    ITexture CreatePixelTexture(byte[] data, int width, int height, TextureFormat format, TextureFlags flags);

    ITexture CreateEmptyTexture(int width, int height, TextureFormat format, TextureFlags flags);

    void Render(IRenderPass pass, RenderState state);
}
