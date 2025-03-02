namespace Staple.Internal;

internal sealed class StapleSerializerField
{
    public string typeName;
    public object value;

    public SerializableStapleAssetParameter ToSerializableParameter()
    {
        return new()
        {
            typeName = typeName,
            value = value is StapleSerializerContainer container ? container.ToSerializableContainer() : value,
        };
    }
}
