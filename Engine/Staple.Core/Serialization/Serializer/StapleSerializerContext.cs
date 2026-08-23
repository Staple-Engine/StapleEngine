using System.Reflection;

namespace Staple.Internal;

internal sealed class StapleSerializerContext
{
    public delegate object Instance();
    public delegate void SetField(FieldInfo field, string type, object value);
    public delegate void SetProperty(PropertyInfo property, string type, object value);

    public readonly Instance instance;
    public readonly SetField setField;
    public readonly SetProperty setProperty;

    public StapleSerializerContext(Instance instance, SetField setField, SetProperty setProperty)
    {
        this.instance = instance;
        this.setField = setField;
        this.setProperty = setProperty;
    }
}
