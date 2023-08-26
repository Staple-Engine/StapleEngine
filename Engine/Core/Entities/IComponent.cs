using System.Diagnostics.CodeAnalysis;

namespace Staple
{
    /// <summary>
    /// Component interface. Used to denote what is a component.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public interface IComponent
    {
    }
}
