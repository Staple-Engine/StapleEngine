using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Staple.Internal;

internal partial class ComputeShaderResource
{
    internal readonly ShaderMetadata metadata;

    internal ShaderUniformInfo[] uniforms = [];
    internal readonly IntLookupCache<int> uniformIndices = new();

    internal readonly byte[] shaderSource = [];

    internal readonly ComputeShaderMetrics metrics;

    internal readonly ShaderUniformContainer uniformContainer;

    internal readonly Dictionary<string, ShaderUniformField> fields = [];

    internal IShaderProgram program;

    public GuidHasher Guid = new();

    [GeneratedRegex("\\[([0-9]+)\\]")]
    private static partial Regex UniformCountRegex();

    private static readonly Regex uniformCountRegex = UniformCountRegex();

    internal ComputeShaderResource(SerializableShader shader, Dictionary<string, SerializableShaderData> entries)
    {
        metadata = shader.metadata;

        var entry = entries.FirstOrDefault().Value;

        shaderSource = entry.computeShader ?? [];
        metrics = entry.computeMetrics ?? new();
        uniformContainer = entry.computeUniforms;

        foreach (var uniform in entry.computeUniforms.uniforms)
        {
            if ((uniform.fields?.Length ?? 0) == 0)
            {
                //TODO: Actual uniforms
            }
            else
            {
                foreach (var field in uniform.fields)
                {
                    fields.AddOrSetKey(field.name, field);
                }
            }
        }
    }

    private static string NormalizeUniformName(string name, ShaderUniformType type)
    {
        if (uniformCountRegex.IsMatch(name))
        {
            name = name.Replace(uniformCountRegex.Match(name).Value, string.Empty);
        }

        return type switch
        {
            ShaderUniformType.Int or ShaderUniformType.Float or ShaderUniformType.Vector2 or ShaderUniformType.Vector3 => $"{name}_uniform",
            _ => name
        };
    }

    private static int NormalizeUniformCount(string name)
    {
        if (!uniformCountRegex.IsMatch(name))
        {
            return 1;
        }

        var match = uniformCountRegex.Match(name);

        if (match.Groups.Count == 2)
        {
            return int.TryParse(match.Groups[1].Value, out var value) ? value : 1;
        }

        return 1;
    }

    internal unsafe bool Create()
    {
        if ((shaderSource?.Length ?? 0) == 0)
        {
            return false;
        }

        program = RenderSystem.Backend.CreateShaderCompute(shaderSource, metrics);

        if (program == null)
        {
            return false;
        }

        /*
        if (uniforms.Length > 0)
        {
            foreach (var uniform in uniforms)
            {
                uniform.Create();
            }
        }
        else
        {
            foreach (var uniform in metadata.uniforms)
            {
                AddUniform(Shader.DefaultUniform.FromShaderUniform(uniform));
            }

            void EnsureUniform(Shader.DefaultUniform u)
            {
                var uniform = GetUniform(u.name.GetHashCode());

                if (uniform == null)
                {
                    AddUniform(u);
                }
            }

            foreach (var uniform in DefaultUniforms)
            {
                EnsureUniform(uniform);
            }
        }

        Disposed = false;

        return true;
        */

        return false;
    }

    internal void AddUniform(ShaderResource.DefaultUniform uniform)
    {
        var normalizedName = NormalizeUniformName(uniform.name, uniform.type);
        var nameHash = uniform.name.GetHashCode();
        var normalizedHash = normalizedName.GetHashCode();

        var uniformIndex = uniformIndices.IndexOf(nameHash);

        if (uniformIndex >= 0)
        {
            return;
        }

        uniformIndex = uniformIndices.IndexOf(normalizedHash);

        if (uniformIndex >= 0)
        {
            return;
        }

        var u = new ShaderUniformInfo()
        {
            uniform = new()
            {
                name = normalizedName,
                type = uniform.type,
                attribute = uniform.attribute,
                variant = uniform.variant,
                defaultValue = uniform.defaultValue,
            },
            handle = new(normalizedName),
            count = NormalizeUniformCount(uniform.name),
        };

        var i = uniforms.Length;

        uniformIndices.Add(normalizedHash, i);
        uniforms = uniforms.Concat([u]).ToArray();

        if (uniformIndices.IndexOf(nameHash) < 0)
        {
            uniformIndices.Add(nameHash, i);

            uniforms = uniforms.Concat([new()
            {
                count = u.count,
                isAlias = true,
                uniform = new()
                {
                    name = u.uniform.name,
                    type = uniform.type,
                    attribute = uniform.attribute,
                    variant = uniform.variant,
                    defaultValue = uniform.defaultValue,
                    slot = uniform.slot,
                },
                handle = new(normalizedName),
            }]).ToArray();
        }
    }

    public void Destroy()
    {
        /*
        if(programHandle.Valid)
        {
            bgfx.destroy_program(programHandle);

            programHandle = new()
            {
                idx = ushort.MaxValue,
            };
        }
        */

        /*
        foreach (var uniform in uniforms)
        {
            if (uniform.isAlias)
            {
                uniform.handle.idx = ushort.MaxValue;

                continue;
            }

            if (uniform.handle.Valid)
            {
                bgfx.destroy_uniform(uniform.handle);

                uniform.handle.idx = ushort.MaxValue;
            }
        }
        */
    }
}
