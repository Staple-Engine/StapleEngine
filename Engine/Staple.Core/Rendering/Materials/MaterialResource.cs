using System.Collections.Generic;

namespace Staple.Internal;

internal class MaterialResource
{
    internal Shader shader;
    internal MaterialMetadata metadata;

    internal readonly Dictionary<MaterialResourceParameter, int> vertexTextureBindings = [];
    internal readonly Dictionary<MaterialResourceParameter, int> fragmentTextureBindings = [];

    internal Texture[] vertexSamplers;

    internal Texture[] fragmentSamplers;

    internal Dictionary<StringID, MaterialResourceParameter> parameters = [];

    internal Dictionary<StringID, MaterialResourceParameter> instanceParameters = [];

    public GuidHasher Guid = new();

    public MaterialResource Clone()
    {
        var outValue = new MaterialResource()
        {
            shader = shader,
            metadata = metadata,
            vertexSamplers = MemoryUtils.SafeCloneArray(vertexSamplers),
            fragmentSamplers = MemoryUtils.SafeCloneArray(fragmentSamplers),
        };

        outValue.Guid.Guid = Guid.Guid;

        foreach(var pair in vertexTextureBindings)
        {
            outValue.vertexTextureBindings.Add(pair.Key, pair.Value);
        }

        foreach (var pair in fragmentTextureBindings)
        {
            outValue.fragmentTextureBindings.Add(pair.Key, pair.Value);
        }

        foreach (var parameter in parameters)
        {
            outValue.parameters.Add(parameter.Key, parameter.Value.Clone());
        }

        foreach (var parameter in instanceParameters)
        {
            outValue.instanceParameters.Add(parameter.Key, parameter.Value.Clone());
        }

        return outValue;
    }
}
