using System;

namespace Staple;

/// <summary>
/// Adds a required component constraint to a component. Only has an effect when applied to a component.
/// </summary>
/// <param name="type">The type of the component</param>
/// <param name="field">The name of the field that should be updated with the component, if any</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class RequireComponentAttribute(Type type, string field) : Attribute
{
    public readonly Type type = type;
    public readonly string field = field;
}
