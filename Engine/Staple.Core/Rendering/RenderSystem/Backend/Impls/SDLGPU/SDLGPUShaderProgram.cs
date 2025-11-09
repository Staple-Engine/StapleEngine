using SDL3;
using System;
using System.Collections.Generic;

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
    public Dictionary<byte, byte[]> uniformValues;
    public ShaderUniformContainer uniforms;

    public SDLGPUShaderProgram(nint device, nint vertex, nint fragment, VertexAttribute[] vertexAttributes,
        ShaderUniformContainer uniforms, Dictionary<byte, byte[]> uniformValues)
    {
        Type = ShaderType.VertexFragment;

        this.device = device;
        this.vertex = vertex;
        this.fragment = fragment;
        this.vertexAttributes = vertexAttributes;
        this.uniforms = uniforms;
        this.uniformValues = uniformValues;
    }

    public SDLGPUShaderProgram(nint device, nint compute, ShaderUniformContainer uniforms, Dictionary<byte, byte[]> uniformValues)
    {
        Type = ShaderType.Compute;

        this.device = device;
        this.compute = compute;
        this.uniforms = uniforms;
        this.uniformValues = uniformValues;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(device, vertex, fragment, compute, disposed);
    }

    public bool TryGetUniformData(byte binding, out byte[] data)
    {
        return uniformValues.TryGetValue(binding, out data);
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
