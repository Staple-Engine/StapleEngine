using System;

namespace Staple;

/// <summary>
/// Apply this to a component to make the engine attempt to find a `entity` field and assign the related entity to that component
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class AutoAssignEntityAttribute : Attribute
{
}
