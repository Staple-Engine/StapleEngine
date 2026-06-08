namespace Staple;

/// <summary>
/// Skinned mesh attachment component.
/// Automatically syncs the entity's transform with a bone for the specified mesh.
/// </summary>
public sealed class SkinnedMeshAttachment : IComponent
{
    /// <summary>
    /// The main mesh that contains the skeleton
    /// </summary>
    public Mesh mesh;

    /// <summary>
    /// The name of the bone to attach to
    /// </summary>
    public string boneName;

    /// <summary>
    /// The cached local node index
    /// </summary>
    internal int nodeIndex = -1;

    /// <summary>
    /// The cached bone name, in case it changes
    /// </summary>
    internal string previousBoneName;

    /// <summary>
    /// The animator this should sync with, if any
    /// </summary>
    internal EntityQuery<SkinnedMeshAnimator> animator;
}
