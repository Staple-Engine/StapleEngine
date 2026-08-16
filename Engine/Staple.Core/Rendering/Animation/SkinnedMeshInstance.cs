using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple;

public class SkinnedMeshInstance : IComponent, IComponentDisposable
{
    /// <summary>
    /// The mesh asset used for this
    /// </summary>
    internal Mesh mesh;

    /// <summary>
    /// Cached bone matrices
    /// </summary>
    internal Matrix4x4[] boneMatrices;

    /// <summary>
    /// Cached bone buffer
    /// </summary>
    internal VertexBuffer boneBuffer;

    /// <summary>
    /// Cached transforms
    /// </summary>
    internal Transform[] transformCache;

    /// <summary>
    /// Cached nodes
    /// </summary>
    internal MeshAsset.Node[] nodeCache;

    /// <summary>
    /// Timer for updating transforms
    /// </summary>
    internal float transformUpdateTimer;

    /// <summary>
    /// List of modifiers
    /// </summary>
    internal EntityQuery<Transform, ISkinModifier> modifiers;

    /// <summary>
    /// Whether we have an animator
    /// </summary>
    internal EntityQuery<SkinnedMeshAnimator> animator;

    /// <summary>
    /// Our renderers
    /// </summary>
    internal EntityQuery<SkinnedMeshRenderer> renderers;

    /// <summary>
    /// Our blend shape names
    /// </summary>
    internal string[] blendShapeNames;

    /// <summary>
    /// Our blend shape weights
    /// </summary>
    internal float[] blendShapeWeights;

    /// <summary>
    /// Gets the name of each blend shape in this renderer
    /// </summary>
    public Span<string> BlendShapeNames => blendShapeNames != null ? blendShapeNames.AsSpan() : null;

    /// <summary>
    /// Gets the weights of each blend shape in this renderer
    /// </summary>
    public Span<float> BlendShapeWeights => blendShapeWeights != null ? blendShapeWeights.AsSpan() : default;

    internal void UpdateBlendShapeData()
    {
        if(renderers != null)
        {
            var data = new Dictionary<string, float>();

            foreach(var renderer in renderers.Contents)
            {
                if((renderer.blendShapeWeights?.Count ?? 0) > 0)
                {
                    for(var i = 0; i < renderer.blendShapeNames.Length; i++)
                    {
                        data.AddOrSetKey(renderer.blendShapeNames[i], renderer.blendShapeWeights[i]);
                    }
                }
            }

            blendShapeNames = new string[data.Count];
            blendShapeWeights = new float[data.Count];

            var counter = 0;

            foreach(var pair in data)
            {
                blendShapeNames[counter] = pair.Key;
                blendShapeWeights[counter++] = pair.Value;
            }

            foreach (var renderer in renderers.Contents)
            {
                foreach(var pair in data)
                {
                    var index = renderer.GetBlendShapeIndex(pair.Key);

                    if(index < 0)
                    {
                        continue;
                    }

                    renderer.blendShapeWeights[index] = pair.Value;

                    renderer.needsUpdate = true;
                }
            }
        }
    }

    /// <summary>
    /// Sets a blend shape's weight
    /// </summary>
    /// <param name="name">The name of the blend shape</param>
    /// <param name="weight">The weight</param>
    public void SetBlendShapeWeight(string name, float weight)
    {
        if(blendShapeNames == null)
        {
            return;
        }

        var index = Array.IndexOf(blendShapeNames, name);

        if (index < 0 || index > blendShapeNames.Length)
        {
            return;
        }

        blendShapeWeights[index] = weight;

        if (renderers != null)
        {
            foreach (var renderer in renderers.Contents)
            {
                index = renderer.GetBlendShapeIndex(name);

                if(index < 0)
                {
                    continue;
                }

                renderer.blendShapeWeights[index] = weight;

                renderer.needsUpdate = true;
            }
        }
    }

    /// <summary>
    /// Attempts to get the index of a blend shape by name
    /// </summary>
    /// <param name="name">The name of the blend shape</param>
    /// <returns>The blend shape index, or -1 if invalid</returns>
    public int GetBlendShapeIndex(string name)
    {
        if(blendShapeNames == null)
        {
            return -1;
        }

        return Array.IndexOf(blendShapeNames, name);
    }

    /// <summary>
    /// Gets a blend shape's weight
    /// </summary>
    /// <param name="name">The name of the blend shape</param>
    /// <returns>The weight, or 0</returns>
    public float GetBlendShapeWeight(string name)
    {
        if(blendShapeNames == null)
        {
            return 0;
        }

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
        if (blendShapeNames == null || index < 0 || index > blendShapeNames.Length)
        {
            return 0;
        }

        return blendShapeWeights[index];
    }

    /// <summary>
    /// Attempts to get a blend shape's weight
    /// </summary>
    /// <param name="name">The name of the blend shape</param>
    /// <param name="weight">The weight, if valid</param>
    /// <returns>Whether the blend shape was found</returns>
    public bool TryGetBlendShapeWeight(string name, out float weight)
    {
        var index = GetBlendShapeIndex(name);

        if(index < 0)
        {
            weight = default;

            return false;
        }

        weight = blendShapeWeights[index];

        return true;
    }

    public void DisposeComponent()
    {
        boneBuffer?.Destroy();

        boneBuffer = null;
    }
}
