using System;

namespace Staple;

/// <summary>
/// Apply this to a component to attempt to find a `entity` field and assign the related entity to that component when it's created.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class AutoAssignEntityAttribute : Attribute
{
}
