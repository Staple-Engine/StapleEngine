using SDL3;
using System;
using System.Collections.Generic;
using Standart.Hash.xxHash;

namespace Staple.Internal;

internal class SDLGPUShaderProgram : IShaderProgram
{
    public ShaderType Type { get; private set; }
    public int StateKey => HashCode.Combine(device, vertex, fragment, compute, disposed);

    public readonly nint device;
    public nint vertex;
    public nint fragment;
    public nint compute;
    public bool disposed = false;
    public Dictionary<ShaderUniformMapping, byte[]> vertexUniforms;
    public Dictionary<ShaderUniformMapping, byte[]> fragmentUniforms;
    public Dictionary<ShaderUniformMapping, byte[]> computeUniforms;
    public readonly Dictionary<byte, ulong> vertexDataHashes = [];
    public readonly Dictionary<byte, ulong> fragmentDataHashes = [];
    public readonly Dictionary<byte, ulong> computeDataHashes = [];

    public SDLGPUShaderProgram(nint device, nint vertex, nint fragment, ShaderUniformContainer vertexUniforms,
        ShaderUniformContainer fragmentUniforms)
    {
        Type = ShaderType.VertexFragment;

        this.device = device;
        this.vertex = vertex;
        this.fragment = fragment;
        this.vertexUniforms = [];
        this.fragmentUniforms = [];

        foreach(var uniform in vertexUniforms.uniforms)
        {
            this.vertexUniforms.Add(uniform, new byte[uniform.size]);
        }

        foreach (var uniform in fragmentUniforms.uniforms)
        {
            this.fragmentUniforms.Add(uniform, new byte[uniform.size]);
        }
    }

    public SDLGPUShaderProgram(nint device, nint compute, ShaderUniformContainer uniforms)
    {
        Type = ShaderType.Compute;

        this.device = device;
        this.compute = compute;
        
        computeUniforms = [];

        foreach(var uniform in uniforms.uniforms)
        {
            computeUniforms.Add(uniform, new byte[uniform.size]);
        }
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(device, vertex, fragment, compute, disposed);
    }

    public void ClearUniformHashes()
    {
        vertexDataHashes.Clear();
        fragmentDataHashes.Clear();
        computeDataHashes.Clear();
    }

    private static ulong MakeDataHash(Span<byte> data)
    {
        if(data.IsEmpty)
        {
            return 0;
        }

        return xxHash64.ComputeHash(data, data.Length);
    }

    public bool ShouldPushVertexUniform(byte binding, Span<byte> data)
    {
        var hash = MakeDataHash(data);

        if(vertexDataHashes.TryGetValue(binding, out var last) == false)
        {
            vertexDataHashes.Add(binding, hash);

            return true;
        }

        if(last != hash)
        {
            vertexDataHashes[binding] = hash;

            return true;
        }

        return false;
    }

    public bool ShouldPushFragmentUniform(byte binding, Span<byte> data)
    {
        var hash = MakeDataHash(data);

        if (fragmentDataHashes.TryGetValue(binding, out var last) == false)
        {
            fragmentDataHashes.Add(binding, hash);

            return true;
        }

        if (last != hash)
        {
            fragmentDataHashes[binding] = hash;

            return true;
        }

        return false;
    }

    public bool ShouldPushComputeUniform(byte binding, Span<byte> data)
    {
        var hash = MakeDataHash(data);

        if (computeDataHashes.TryGetValue(binding, out var last) == false)
        {
            computeDataHashes.Add(binding, hash);

            return true;
        }

        if (last != hash)
        {
            computeDataHashes[binding] = hash;

            return true;
        }

        return false;
    }

    public bool TryGetVertexUniformData(ShaderUniformField field, out byte[] data)
    {
        if(disposed || Type != ShaderType.VertexFragment)
        {
            data = default;

            return false;
        }

        foreach(var pair in vertexUniforms)
        {
            foreach(var f in pair.Key.fields)
            {
                if(f == field)
                {
                    data = pair.Value;

                    return true;
                }
            }
        }

        data = default;

        return false;
    }

    public bool TryGetVertexUniformData(ShaderUniformMapping mapping, out byte[] data)
    {
        if (disposed || Type != ShaderType.VertexFragment)
        {
            data = default;

            return false;
        }

        foreach (var pair in vertexUniforms)
        {
            if(pair.Key == mapping)
            {
                data = pair.Value;

                return true;
            }
        }

        data = default;

        return false;
    }

    public bool TryGetFragmentUniformData(ShaderUniformField field, out byte[] data)
    {
        if (disposed || Type != ShaderType.VertexFragment)
        {
            data = default;

            return false;
        }

        foreach (var pair in fragmentUniforms)
        {
            foreach (var f in pair.Key.fields)
            {
                if (f == field)
                {
                    data = pair.Value;

                    return true;
                }
            }
        }

        data = default;

        return false;
    }

    public bool TryGetFragmentUniformData(ShaderUniformMapping mapping, out byte[] data)
    {
        if (disposed || Type != ShaderType.VertexFragment)
        {
            data = default;

            return false;
        }

        foreach (var pair in fragmentUniforms)
        {
            if (pair.Key == mapping)
            {
                data = pair.Value;

                return true;
            }
        }

        data = default;

        return false;
    }

    public bool TryGetComputeUniformData(ShaderUniformField field, out byte[] data)
    {
        if (disposed || Type != ShaderType.Compute)
        {
            data = default;

            return false;
        }

        foreach (var pair in computeUniforms)
        {
            foreach (var f in pair.Key.fields)
            {
                if (f == field)
                {
                    data = pair.Value;

                    return true;
                }
            }
        }

        data = default;

        return false;
    }

    public bool TryGetComputeUniformData(ShaderUniformMapping mapping, out byte[] data)
    {
        if (disposed || Type != ShaderType.Compute)
        {
            data = default;

            return false;
        }

        foreach (var pair in computeUniforms)
        {
            if (pair.Key == mapping)
            {
                data = pair.Value;

                return true;
            }
        }

        data = default;

        return false;
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

                SDL.ReleaseGPUShader(device, vertex);
                SDL.ReleaseGPUShader(device, fragment);

                vertex = nint.Zero;
                fragment = nint.Zero;

                break;

            case ShaderType.Compute:

                SDL.ReleaseGPUShader(device, compute);

                compute = nint.Zero;

                break;
        }
    }
}
