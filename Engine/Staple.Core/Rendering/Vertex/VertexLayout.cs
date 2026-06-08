using Staple.Internal;

namespace Staple;

/// <summary>
/// Manages a vertex layout
/// </summary>
public abstract class VertexLayout
{
    /// <summary>
    /// The mesh components for this layout
    /// </summary>
    public MeshAssetComponent Components { get; protected set; }

    /// <summary>
    /// The byte count of a vertex
    /// </summary>
    public int Stride { get; protected set; }
}
