using SDL3;
using System;

namespace Staple.Internal;

internal class SDLGPUShaderProgram : IShaderProgram
{
    public ShaderType Type { get; private set; }

    public readonly nint device;
    public nint vertex;
    public nint fragment;
    public nint compute;
    public bool disposed = false;
    public VertexAttribute[] vertexAttributes;

    public SDLGPUShaderProgram(nint device, nint vertex, nint fragment, VertexAttribute[] vertexAttributes)
    {
        Type = ShaderType.VertexFragment;

        this.device = device;
        this.vertex = vertex;
        this.fragment = fragment;
        this.vertexAttributes = vertexAttributes;
    }

    public SDLGPUShaderProgram(nint device, nint compute)
    {
        Type = ShaderType.Compute;

        this.device = device;
        this.compute = compute;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(device, vertex, fragment, compute, disposed);
    }

    public void Destroy()
    {
        if(disposed)
        {
            return;
        }

        disposed = true;

        switch(Type)
        {
            case ShaderType.VertexFragment:

                SDL.SDL_ReleaseGPUShader(device, vertex);
                SDL.SDL_ReleaseGPUShader(device, fragment);

                vertex = nint.Zero;
                fragment = nint.Zero;

                break;

            case ShaderType.Compute:

                SDL.SDL_ReleaseGPUShader(device, compute);

                compute = nint.Zero;

                break;
        }
    }
}
