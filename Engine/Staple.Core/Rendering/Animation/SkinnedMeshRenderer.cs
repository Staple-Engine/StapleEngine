using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Staple;

/// <summary>
/// Skinned Mesh Renderer component
/// </summary>
public sealed class SkinnedMeshRenderer : Renderable, IComponentDisposable
{
    /// <summary>
    /// The mesh used for this
    /// </summary>
    public Mesh mesh;

    /// <summary>
    /// Whether to disable skinning entirely for this renderer
    /// </summary>
    public bool disableSkinning = false;

    /// <summary>
    /// Blend shape weights for this renderer
    /// </summary>
    [SerializeField]
    internal List<float> blendShapeWeights = [];

    /// <summary>
    /// The name of the blend shapes in this renderer
    /// </summary>
    internal string[] blendShapeNames = [];

    /// <summary>
    /// Skinned mesh instance query
    /// </summary>
    internal EntityQuery<SkinnedMeshInstance> instance;

    /// <summary>
    /// Stores the blend shapes of the mesh, if any
    /// </summary>
    internal VertexBuffer blendShapeBuffer;

    /// <summary>
    /// Stores the blend shape parameters of the mesh, if any
    /// </summary>
    internal VertexBuffer blendShapeParameterBuffer;

    /// <summary>
    /// Whether this component needs to be updated
    /// </summary>
    internal bool needsUpdate = true;

    /// <summary>
    /// Gets the name of each blend shape in this renderer
    /// </summary>
    public Span<string> BlendShapeNames => blendShapeNames.AsSpan();

    /// <summary>
    /// Gets the weights of each blend shape in this renderer
    /// </summary>
    public Span<float> BlendShapeWeights => CollectionsMarshal.AsSpan(blendShapeWeights);

    /// <summary>
    /// Sets a blend shape's weight
    /// </summary>
    /// <param name="name">The name of the blend shape</param>
    /// <param name="weight">The weight</param>
    public void SetBlendShapeWeight(string name, float weight)
    {
        var index = Array.IndexOf(blendShapeNames, name);

        SetBlendShapeWeight(index, weight);
    }

    /// <summary>
    /// Sets a blend shape's weight
    /// </summary>
    /// <param name="index">The index of the blend shape</param>
    /// <param name="weight">The weight</param>
    public void SetBlendShapeWeight(int index, float weight)
    {
        if (index < 0 || index > blendShapeNames.Length)
        {
            return;
        }

        if(instance?.Content is SkinnedMeshInstance i)
        {
            i.SetBlendShapeWeight(blendShapeNames[index], weight);
        }
        else
        {
            blendShapeWeights[index] = weight;

            needsUpdate = true;
        }
    }

    /// <summary>
    /// Attempts to get the index of a blend shape by name
    /// </summary>
    /// <param name="name">The name of the blend shape</param>
    /// <returns>The blend shape index, or -1 if invalid</returns>
    public int GetBlendShapeIndex(string name)
    {
        return Array.IndexOf(blendShapeNames, name);
    }

    /// <summary>
    /// Gets a blend shape's weight
    /// </summary>
    /// <param name="name">The name of the blend shape</param>
    /// <returns>The weight, or 0</returns>
    public float GetBlendShapeWeight(string name)
    {
        var index = Array.IndexOf(blendShapeNames, name);

        return GetBlendShapeWeight(index);
    }

    /// <summary>
    /// Gets a blend shape's weight
    /// </summary>
    /// <param name="index">The index of the blend shape</param>
    /// <returns>The weight, or 0</returns>
    public float GetBlendShapeWeight(int index)
    {
        if (index < 0 || index > blendShapeNames.Length)
        {
            return 0;
        }

        return blendShapeWeights[index];
    }

    public void DisposeComponent()
    {
        ///Must not dispose of the <see cref="blendShapeBuffer"/>, since it's cached internally.
        ///The parameter buffer is fine since it's local to this component

        blendShapeParameterBuffer?.Destroy();

        blendShapeParameterBuffer = null;
    }
}
