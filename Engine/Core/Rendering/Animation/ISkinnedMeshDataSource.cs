namespace Staple;

/// <summary>
/// A data source for providing a bone matrix buffer for a skinned renderer
/// </summary>
public interface ISkinnedMeshDataSource : IComponent
{
    /// <summary>
    /// Gets a buffer containing the skinning data for a renderer
    /// </summary>
    /// <param name="renderer">The renderer we want to get the data for</param>
    /// <returns>The vertex buffer, or null</returns>
    VertexBuffer GetSkinningBuffer(SkinnedMeshRenderer renderer);
}
