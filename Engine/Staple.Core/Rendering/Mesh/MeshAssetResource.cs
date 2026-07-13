using System.Collections.Generic;

namespace Staple.Internal;

internal class MeshAssetResource
{
    /// <summary>
    /// List of each mesh in the asset
    /// </summary>
    public MeshAsset.MeshInfo[] meshes = [];

    /// <summary>
    /// The nodes of the transform tree
    /// </summary>
    public MeshAsset.Node[] nodes;

    /// <summary>
    /// List of all animations
    /// </summary>
    public readonly Dictionary<string, MeshAsset.Animation> animations = [];

    /// <summary>
    /// The lighting type for this mesh
    /// </summary>
    public MaterialLighting lighting;

    /// <summary>
    /// The frame rate of the animations in this mesh
    /// </summary>
    public int frameRate = 30;

    /// <summary>
    /// Whether to sync the animation to the screen refresh rate
    /// </summary>
    public bool syncAnimationToRefreshRate = false;

    /// <summary>
    /// 3D bounds of the mesh
    /// </summary>
    public AABB Bounds { get; internal set; }

    /// <summary>
    /// The amount of bones in the meshes within this MeshAsset
    /// </summary>
    public int BoneCount { get; internal set; }

    public GuidHasher Guid = new();
}
