using System;

namespace Staple;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class ComponentIconAttribute : Attribute
{
    public string path;

    public ComponentIconAttribute(string path)
    {
        this.path = path;
    }
}
