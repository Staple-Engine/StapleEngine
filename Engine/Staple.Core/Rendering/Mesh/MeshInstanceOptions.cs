using System;

namespace Staple;

/// <summary>
/// Options when instancing a mesh
/// </summary>
[Flags]
public enum MeshInstanceOptions
{
    /// <summary>
    /// No options in particular
    /// </summary>
    None = (1 << 0),
    /// <summary>
    /// Make skinned meshes unskinned
    /// </summary>
    MakeUnskinned = (1 << 1),
}
