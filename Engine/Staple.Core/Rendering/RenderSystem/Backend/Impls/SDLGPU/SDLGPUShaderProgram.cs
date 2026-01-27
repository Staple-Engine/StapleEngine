using SDL;
using System;

namespace Staple.Internal;

internal unsafe class SDLGPUShaderProgram : IShaderProgram
{
    public ShaderType Type { get; }

    public int StateKey => HashCode.Combine((nint)device, (nint)vertex, (nint)fragment, (nint)compute, disposed);

    public readonly SDL_GPUDevice *device;
    public SDL_GPUShader *vertex;
    public SDL_GPUShader *fragment;
    public SDL_GPUComputePipeline *compute;
    public bool disposed = false;

    public SDLGPUShaderProgram(SDL_GPUDevice *device, SDL_GPUShader *vertex, SDL_GPUShader *fragment)
    {
        Type = ShaderType.VertexFragment;

        this.device = device;
        this.vertex = vertex;
        this.fragment = fragment;
    }

    public SDLGPUShaderProgram(SDL_GPUDevice* device, SDL_GPUComputePipeline* compute)
    {
        Type = ShaderType.Compute;

        this.device = device;
        this.compute = compute;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((nint)device, (nint)vertex, (nint)fragment, (nint)compute, disposed);
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

                SDL3.SDL_ReleaseGPUShader(device, vertex);
                SDL3.SDL_ReleaseGPUShader(device, fragment);

                vertex = null;
                fragment = null;

                break;

            case ShaderType.Compute:

                SDL3.SDL_ReleaseGPUComputePipeline(device, compute);

                compute = null;

                break;
        }
    }
}
