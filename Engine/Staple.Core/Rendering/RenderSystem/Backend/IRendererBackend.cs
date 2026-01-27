using System;
using System.Numerics;

namespace Staple.Internal;

internal interface IRendererBackend
{
    bool SupportsTripleBuffering { get; }

    bool SupportsHDRColorSpace { get; }

    bool SupportsLinearColorSpace { get; }

    TextureFormat? DepthStencilFormat { get; }

    TextureFormat SwapchainFormat { get; }

    BufferAttributeContainer StaticMeshData { get; }

    bool Initialize(RendererType renderer, bool debug, IRenderWindow window, RenderModeFlags renderFlags);

    void Destroy();

    void BeginFrame();

    void EndFrame();

    void UpdateRenderMode(RenderModeFlags flags);

    void UpdateViewport(int width, int height);

    void BeginRenderPass(RenderTarget target, CameraClearMode clear, Color clearColor, Vector4 viewport,
        in Matrix4x4 view, in Matrix4x4 projection);

    VertexLayoutBuilder CreateVertexLayoutBuilder();

    IShaderProgram CreateShaderVertexFragment(byte[] vertex, byte[] fragment,
        VertexFragmentShaderMetrics vertexMetrics, VertexFragmentShaderMetrics fragmentMetrics);

    IShaderProgram CreateShaderCompute(byte[] compute, ComputeShaderMetrics computeMetrics);

    VertexBuffer CreateVertexBuffer(Span<byte> data, VertexLayout layout, RenderBufferFlags flags);

    VertexBuffer CreateVertexBuffer<T>(Span<T> data, VertexLayout layout, RenderBufferFlags flags) where T : unmanaged;

    IndexBuffer CreateIndexBuffer(Span<ushort> data, RenderBufferFlags flags);

    IndexBuffer CreateIndexBuffer(Span<uint> data, RenderBufferFlags flags);

    void UpdateVertexBuffer(ResourceHandle<VertexBuffer> buffer, Span<byte> data);

    void UpdateIndexBuffer(ResourceHandle<IndexBuffer> buffer, Span<ushort> data);

    void UpdateIndexBuffer(ResourceHandle<IndexBuffer> buffer, Span<uint> data);

    void DestroyVertexBuffer(ResourceHandle<VertexBuffer> buffer);

    void DestroyIndexBuffer(ResourceHandle<IndexBuffer> buffer);

    bool SupportsTextureFormat(TextureFormat format, TextureFlags flags);

    ITexture CreateTextureAssetTexture(SerializableTexture asset, TextureFlags flags);

    ITexture CreatePixelTexture(byte[] data, int width, int height, TextureFormat format, TextureFlags flags);

    ITexture CreateEmptyTexture(int width, int height, TextureFormat format, TextureFlags flags);

    void ReadTexture(ITexture texture, Action<byte[]> onComplete);

    void Render(RenderState state);

    void RenderStatic(RenderState state, Span<MultidrawEntry> entries);

    void RenderTransient<T>(Span<T> vertices, VertexLayout layout, Span<ushort> indices, RenderState state) where T : unmanaged;

    void RenderTransient<T>(Span<T> vertices, VertexLayout layout, Span<uint> indices, RenderState state) where T : unmanaged;

    void UpdateStaticMeshVertexBuffer<T>(BufferAttributeSource<T, VertexBuffer> buffer) where T : unmanaged;

    void UpdateStaticMeshIndexBuffer(BufferAttributeSource<int, IndexBuffer> buffer);
}
