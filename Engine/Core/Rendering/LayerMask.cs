using MessagePack;
using System.Collections.Generic;

namespace Staple;

/// <summary>
/// A layer mask. Can contain zero or more layers.
/// </summary>
public struct LayerMask
{
    [IgnoreMember]
    internal static List<string> AllLayers = new();

    [IgnoreMember]
    internal static List<string> AllSortingLayers = new();

    /// <summary>
    /// The layer mask's value
    /// </summary>
    public uint value;

    /// <summary>
    /// Whether this layer mask has a layer
    /// </summary>
    /// <param name="index">The layer's index</param>
    /// <returns>Whether it contains the layer</returns>
    internal bool HasLayer(uint index)
    {
        return (value & (1 << (int)index)) == (1 << (int)index);
    }

    /// <summary>
    /// Adds a layer to this layer mask
    /// </summary>
    /// <param name="index">The layer index</param>
    internal void SetLayer(uint index)
    {
        value |= (uint)((1 << (int)index));
    }

    /// <summary>
    /// Removes a layer from this layer mask
    /// </summary>
    /// <param name="index">The layer index</param>
    internal void RemoveLayer(uint index)
    {
        value &= (uint)(~(1 << (int)index));
    }

    /// <summary>
    /// A default value with all layers.
    /// </summary>
    public static LayerMask Everything
    {
        get
        {
            return new LayerMask()
            {
                value = uint.MaxValue,
            };
        }
    }

    /// <summary>
    /// Gets a mask from layer names.
    /// </summary>
    /// <param name="layerNames">The name of each layer.</param>
    /// <returns>The mask, or 0</returns>
    public static uint GetMask(params string[] layerNames)
    {
        uint mask = 0;

        foreach (var layer in layerNames)
        {
            var index = AllLayers.IndexOf(layer);

            if(index >= 0)
            {
                mask |= (uint)(1 << index);
            }
        }

        return mask;
    }

    /// <summary>
    /// Gets a layer name from an index
    /// </summary>
    /// <param name="index">The layer index</param>
    /// <returns>The layer name, or null</returns>
    public static string LayerToName(int index)
    {
        return index >= 0 && index < AllLayers.Count ? AllLayers[index] : null;
    }

    /// <summary>
    /// Gets a layer index from a name
    /// </summary>
    /// <param name="name">The layer name</param>
    /// <returns>The layer index or -1</returns>
    public static int NameToLayer(string name)
    {
        //Default to first layer
        if(name == null)
        {
            return 0;
        }

        return AllLayers.IndexOf(name);
    }
}
