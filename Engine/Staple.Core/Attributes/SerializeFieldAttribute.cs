using System;

namespace Staple;

/// <summary>
/// Describes an otherwise non-serializable field or property as serializable. Usually used for private fields.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class SerializeFieldAttribute : Attribute
{
}
