namespace Staple;

/// <summary>
/// Used to specify the minimum value for a variable in the inspector
/// </summary>
public class MinAttribute(float minValue) : PropertyAttribute
{
    public float minValue = minValue;
}
