namespace Staple;

/// <summary>
/// Format type for a mesh's indices
/// </summary>
public enum MeshIndexFormat
{
    /// <summary>
    /// 16-bit unsigned integer
    /// </summary>
    UInt16,

    /// <summary>
    /// 32-bit unsigned integer. Useful if your mesh has more than 65535 vertices
    /// </summary>
    UInt32,
}