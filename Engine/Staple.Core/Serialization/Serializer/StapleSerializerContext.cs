using System.Reflection;

namespace Staple.Internal;

internal sealed class StapleSerializerContext(StapleSerializerContext.Instance instance,
    StapleSerializerContext.SetField setField,
    StapleSerializerContext.SetProperty setProperty)
{
    public delegate object Instance();
    public delegate void SetField(FieldInfo field, string type, object value, bool encoded, SerializeOptimizedAttribute.OptionFlags flags);
    public delegate void SetProperty(PropertyInfo property, string type, object value, bool encoded, SerializeOptimizedAttribute.OptionFlags flags);

    public readonly Instance instance = instance;
    public readonly SetField setField = setField;
    public readonly SetProperty setProperty = setProperty;
}
