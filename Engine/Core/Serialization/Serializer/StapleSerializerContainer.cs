using System.Collections.Generic;

namespace Staple.Internal;

internal sealed class StapleSerializerContainer
{
    public Dictionary<string, StapleSerializerField> fields = [];
    public string typeName;

    public SerializableStapleAssetContainer ToSerializableContainer()
    {
        var outValue = new SerializableStapleAssetContainer()
        {
            typeName = typeName,
        };

        foreach(var pair in fields)
        {
            outValue.parameters.Add(pair.Key, pair.Value.ToSerializableParameter());
        }

        return outValue;
    }
}
