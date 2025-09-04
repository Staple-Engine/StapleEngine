using System;

namespace Staple;

/// <summary>
/// Base class to derive custom property attributes
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class PropertyAttribute : Attribute
{
}
