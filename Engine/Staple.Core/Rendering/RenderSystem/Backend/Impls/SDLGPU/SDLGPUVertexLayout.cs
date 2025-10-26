using SDL3;
using System;
using System.Collections.Generic;

namespace Staple.Internal;

internal class SDLGPUVertexLayout : VertexLayout
{
    public readonly SDL.SDL_GPUVertexAttribute[] attributes;
    public readonly List<VertexAttribute> vertexAttributes;

    public SDLGPUVertexLayout(Span<SDL.SDL_GPUVertexAttribute> attributes, List<VertexAttribute> vertexAttributes,
        MeshAssetComponent components, int stride)
    {
        Stride = stride;
        Components = components;

        this.attributes = attributes.ToArray();
        this.vertexAttributes = vertexAttributes;
    }
}
