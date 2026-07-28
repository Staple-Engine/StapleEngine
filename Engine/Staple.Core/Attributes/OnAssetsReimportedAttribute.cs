using System;

namespace Staple;

/// <summary>
/// Apply this to a static method that will be called when assets are reloaded.
/// This typically only happens in the editor.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class OnAssetsReimportedAttribute : Attribute
{
}
