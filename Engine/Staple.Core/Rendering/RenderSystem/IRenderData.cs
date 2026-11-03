using System.Numerics;

namespace Staple;

/// <summary>
/// Interface for keeping render data for a <see cref="RenderSystemBase"/>.
/// Render Data is used to cache render information so you don't need to constantly clear/reallocate every frame.
/// </summary>
public interface IRenderData
{
    /// <summary>
    /// Clear the render data's data.
    /// </summary>
    void Clear();

    /// <summary>
    /// Sorts the renderables of a specific render index.
    /// Called when the camera moves or changes orientation.
    /// </summary>
    /// <param name="renderIndex">The render index</param>
    /// <param name="origin">The current location of the camera</param>
    /// <param name="sortMode">The sorting type</param>
    void Sort(int renderIndex, Vector3 origin, RenderableSortMode sortMode);

    /// <summary>
    /// Submits a specific render index for rendering
    /// </summary>
    /// <param name="renderIndex">The render index to render</param>
    void Submit(int renderIndex);
}
