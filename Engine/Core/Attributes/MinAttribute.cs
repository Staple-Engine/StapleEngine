using System;

namespace Staple;

/// <summary>
/// Used to specify the minimum value for a variable in the inspector
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class MinAttribute : Attribute
{
    public float minValue;

    public MinAttribute(float minValue)
    {
        this.minValue = minValue;
    }
}
