using System;

namespace Staple;

/// <summary>
/// Marks a field or property to serialize optimally. It serializes as hexadecimal data in text form.
/// This only applies to fields that are arrays and Lists of unmanaged data types (excluding strings)
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class SerializeOptimizedAttribute(SerializeOptimizedAttribute.OptionFlags flags = SerializeOptimizedAttribute.OptionFlags.None) :
    Attribute
{
    [Flags]
    public enum OptionFlags
    {
        None,
        Compress = (1 << 0),
        UseBase64 = (1 << 1),
    }

    public readonly OptionFlags flags = flags;
}
