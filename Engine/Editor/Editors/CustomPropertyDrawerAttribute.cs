using System;

namespace Staple.Editor;

/// <summary>
/// Defines an editor UI drawer for a specific property type
/// Example:
/// [CustomPropertyDrawer(typeof(LayerMask))]
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CustomPropertyDrawerAttribute(Type targetType) : Attribute
{
    public Type targetType = targetType;
}
