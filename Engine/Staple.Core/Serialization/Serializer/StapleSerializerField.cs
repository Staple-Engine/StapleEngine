using System.Collections.Generic;

namespace Staple.Internal;

internal sealed class StapleSerializerField
{
    public string typeName;
    public object value;
    public bool encoded;
    public bool compress;
    public bool useBase64;

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

    public SerializableStapleAssetParameter ToSerializableParameter(StapleSerializationMode mode)
    {
        var v = value is StapleSerializerContainer container ? container.ToSerializableContainer(mode) : value;

        if(value is string && encoded && mode != StapleSerializationMode.Text)
        {
            var flags = SerializeOptimizedAttribute.OptionFlags.None;

            //Do not decompress

            if(useBase64)
            {
                flags |= SerializeOptimizedAttribute.OptionFlags.UseBase64;
            }

            var newValue = StapleSerializer.DecodeOptimizedSerialization(value as string, flags);

            if(newValue != null)
            {
                return new()
                {
                    typeName = typeName,
                    value = newValue,
                    encoded = compress, //Mark as compressed if needed
                    compress = compress,
                };
            }
        }

        return new()
        {
            typeName = typeName,
            value = v,
            encoded = encoded,
            compress = compress,
            useBase64 = useBase64,
        };
    }
}
