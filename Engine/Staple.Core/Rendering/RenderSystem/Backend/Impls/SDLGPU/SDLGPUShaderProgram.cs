using SDL3;
using System;
using System.Collections.Generic;
using Standart.Hash.xxHash;
using System.Runtime.InteropServices;

namespace Staple.Internal;

internal class SDLGPUShaderProgram : IShaderProgram
{
    public ShaderType Type { get; }

    public int StateKey => HashCode.Combine(device, vertex, fragment, compute, disposed);

    public readonly nint device;
    public nint vertex;
    public nint fragment;
    public nint compute;
    public bool disposed = false;
    public readonly Dictionary<byte, ulong> vertexDataHashes = [];
    public readonly Dictionary<byte, ulong> fragmentDataHashes = [];
    public readonly Dictionary<byte, ulong> computeDataHashes = [];
    public readonly Dictionary<ShaderUniformField, byte[]> vertexFields = [];
    public readonly Dictionary<ShaderUniformField, byte[]> fragmentFields = [];
    public readonly Dictionary<ShaderUniformField, byte[]> computeFields = [];
    public readonly Dictionary<ShaderUniformMapping, byte[]> vertexMappings = [];
    public readonly Dictionary<ShaderUniformMapping, byte[]> fragmentMappings = [];
    public readonly Dictionary<ShaderUniformMapping, byte[]> computeMappings = [];

    public SDLGPUShaderProgram(nint device, nint vertex, nint fragment, ShaderUniformContainer vertexUniforms,
        ShaderUniformContainer fragmentUniforms)
    {
        Type = ShaderType.VertexFragment;

        this.device = device;
        this.vertex = vertex;
        this.fragment = fragment;

        foreach(var uniform in vertexUniforms.uniforms)
        {
            var data = new byte[uniform.size];

            vertexMappings.Add(uniform, data);

            foreach(var field in uniform.fields)
            {
                vertexFields.Add(field, data);
            }
        }

        foreach (var uniform in fragmentUniforms.uniforms)
        {
            var data = new byte[uniform.size];

            fragmentMappings.Add(uniform, data);

            foreach (var field in uniform.fields)
            {
                fragmentFields.Add(field, data);
            }
        }
    }

    public SDLGPUShaderProgram(nint device, nint compute, ShaderUniformContainer uniforms)
    {
        Type = ShaderType.Compute;

        this.device = device;
        this.compute = compute;

        foreach(var uniform in uniforms.uniforms)
        {
            var data = new byte[uniform.size];

            computeMappings.Add(uniform, data);

            foreach (var field in uniform.fields)
            {
                computeFields.Add(field, data);
            }
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

        ref var container = ref CollectionsMarshal.GetValueRefOrAddDefault(vertexDataHashes, binding, out var exists);

        if (exists && container == hash)
        {
            return false;
        }
        
        container = hash;

        return true;
    }

    public bool ShouldPushFragmentUniform(byte binding, Span<byte> data)
    {
        var hash = MakeDataHash(data);

        ref var container = ref CollectionsMarshal.GetValueRefOrAddDefault(fragmentDataHashes, binding, out var exists);

        if (exists && container == hash)
        {
            return false;
        }

        container = hash;

        return true;
    }

    public bool ShouldPushComputeUniform(byte binding, Span<byte> data)
    {
        var hash = MakeDataHash(data);

        ref var container = ref CollectionsMarshal.GetValueRefOrAddDefault(computeDataHashes, binding, out var exists);

        if (exists && container == hash)
        {
            return false;
        }
        
        container = hash;

        return true;

    }

    public bool TryGetVertexUniformData(ShaderUniformField field, out byte[] data)
    {
        return vertexFields.TryGetValue(field, out data);
    }

    public bool TryGetVertexUniformData(ShaderUniformMapping mapping, out byte[] data)
    {
        return vertexMappings.TryGetValue(mapping, out data);
    }

    public bool TryGetFragmentUniformData(ShaderUniformField field, out byte[] data)
    {
        return fragmentFields.TryGetValue(field, out data);
    }

    public bool TryGetFragmentUniformData(ShaderUniformMapping mapping, out byte[] data)
    {
        return fragmentMappings.TryGetValue(mapping, out data);
    }

    public bool TryGetComputeUniformData(ShaderUniformField field, out byte[] data)
    {
        return computeFields.TryGetValue(field, out data);
    }

    public bool TryGetComputeUniformData(ShaderUniformMapping mapping, out byte[] data)
    {
        return computeMappings.TryGetValue(mapping, out data);
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
