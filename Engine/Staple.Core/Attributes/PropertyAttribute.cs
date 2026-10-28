using System;

namespace Staple;

/// <summary>
/// Base class to derive custom property attributes
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class PropertyAttribute : Attribute
{
}
