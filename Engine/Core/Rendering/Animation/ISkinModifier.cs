namespace Staple;

/// <summary>
/// A modifier for changing bones in a skinned mesh
/// </summary>
public interface ISkinModifier : IComponent
{
    /// <summary>
    /// Applies a modifier to a specific bone
    /// </summary>
    /// <param name="bone">The bone transform</param>
    /// <param name="wasReset">Whether the transform was just reset</param>
    void Apply(Transform bone, bool wasReset);
}
