namespace Staple;

/// <summary>
/// A modifier for changing bones in a skinned mesh
/// </summary>
[AbstractComponent]
public abstract class SkinModifier : Component
{
    /// <summary>
    /// Applies a modifier to a specific bone
    /// </summary>
    /// <param name="bone">The bone transform</param>
    /// <param name="wasReset">Whether the transform was just reset</param>
    public abstract void Apply(Transform bone, bool wasReset);
}
