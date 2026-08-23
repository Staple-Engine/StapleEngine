using System;

namespace Staple;

/// <summary>
/// Marks a field, property, class, or struct as serializable only while running the editor
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct)]
public class SerializeInEditorAttribute : Attribute
{
}
