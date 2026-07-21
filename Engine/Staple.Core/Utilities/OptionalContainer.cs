namespace Staple;

/// <summary>
/// Used to turn a value-type into a class so it can be optional
/// </summary>
/// <typeparam name="T">The type</typeparam>
/// <param name="value">The value</param>
public class OptionalContainer<T>(T value)
{
    public T value = value;
}
