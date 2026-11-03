using System;

namespace Staple;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class NonSerializedAttribute : Attribute
{
}
