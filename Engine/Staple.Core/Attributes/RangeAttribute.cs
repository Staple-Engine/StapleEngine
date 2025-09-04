namespace Staple;

/// <summary>
/// Used to specify the range of values for a variable in the inspector. The variable will be rendered with a slider.
/// </summary>
public class RangeAttribute(float minValue, float maxValue) : PropertyAttribute
{
    public float minValue = minValue;
    public float maxValue = maxValue;
}
