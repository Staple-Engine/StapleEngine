using System;

namespace Staple;

/// <summary>
/// Marks a field to serialize as base64 on text formats.
/// This only applies to fields that are arrays and Lists of .NET primary types (excluding string arrays).
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class SerializeAsBase64Attribute : Attribute
{
}
