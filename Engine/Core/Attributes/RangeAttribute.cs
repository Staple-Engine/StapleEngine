using System;

namespace Staple;

/// <summary>
/// Used to specify the range of values for a variable in the inspector. The variable will be rendered with a slider.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class RangeAttribute : Attribute
{
    public float minValue;
    public float maxValue;

    public RangeAttribute(float minValue, float maxValue)
    {
        this.minValue = minValue;
        this.maxValue = maxValue;
    }
}