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

    void UpdateRenderMode(RenderModeFlags flags);

    void UpdateViewport(int width, int height);

    IRenderCommand BeginCommand();

    VertexBuffer CreateVertexBuffer(Span<byte> data, VertexLayout layout, RenderBufferFlags flags);

    VertexBuffer CreateVertexBuffer<T>(Span<T> data, VertexLayout layout, RenderBufferFlags flags) where T : unmanaged;

    IndexBuffer CreateIndexBuffer(Span<ushort> data, RenderBufferFlags flags);

    IndexBuffer CreateIndexBuffer(Span<uint> data, RenderBufferFlags flags);

    VertexLayoutBuilder CreateVertexLayoutBuilder();

    IShaderProgram CreateShaderVertexFragment(byte[] vertex, byte[] fragment);

    IShaderProgram CreateShaderCompute(byte[] compute);

    void Render(IRenderPass pass, RenderState state);
}
