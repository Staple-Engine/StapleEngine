using System.Collections.Generic;

namespace Staple.Internal;

internal sealed class StapleSerializerField
{
    public string typeName;
    public object value;

    public object ToRawValue()
    {
        if(value is StapleSerializerContainer container)
        {
            var outDict = new Dictionary<string, object>();

            foreach(var pair in container.fields)
            {
                outDict.Add(pair.Key, pair.Value.ToRawValue());
            }

            return outDict;
        }

        return value;
    }

    public SerializableStapleAssetParameter ToSerializableParameter()
    {
        return new()
        {
            typeName = typeName,
            value = value is StapleSerializerContainer container ? container.ToSerializableContainer() : value,
        };
    }
}
