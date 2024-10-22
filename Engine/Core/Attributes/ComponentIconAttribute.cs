using System;

namespace Staple;

/// <summary>
/// Sets an icon for a component to use in the scene view.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class ComponentIconAttribute : Attribute
{
    public string path;

    public ComponentIconAttribute(string path)
    {
        this.path = path;
    }
}
