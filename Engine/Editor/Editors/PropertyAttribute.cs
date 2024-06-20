using System;

namespace Staple.Editor;

/// <summary>
/// Base class to derive custom property attributes
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class PropertyAttribute : Attribute
{
}
