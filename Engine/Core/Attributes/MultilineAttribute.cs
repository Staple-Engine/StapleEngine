using System;

namespace Staple;

/// <summary>
/// Used in string fields to show a multiline text field in the editor.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class MultilineAttribute : Attribute
{
}
