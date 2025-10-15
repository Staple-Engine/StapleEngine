using SDL3;
using System;

namespace Staple.Internal;

internal class SDLGPUVertexLayout : VertexLayout
{
    public readonly SDL.SDL_GPUVertexAttribute[] attributes;

    public SDLGPUVertexLayout(Span<SDL.SDL_GPUVertexAttribute> attributes, MeshAssetComponent components, int stride)
    {
        Stride = stride;
        Components = components;

        this.attributes = attributes.ToArray();
    }
}
