using System;

namespace Staple;

/// <summary>
/// Disables <see cref="IComponentVersion"/> tracking at the <see cref="World"/> level
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class DisableWorldVersioningUpdatesAttribute : Attribute
{

}
