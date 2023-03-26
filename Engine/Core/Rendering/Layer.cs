using System.Collections.Generic;
using System.Linq;

namespace Staple
{
    /// <summary>
    /// A layer mask. Can contain zero or more layers.
    /// </summary>
    public struct LayerMask
    {
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
            var layers = AppPlayer.active?.appSettings?.layers;

            if (layers == null)
            {
                return 0;
            }

            foreach (var layer in layerNames)
            {
                var index = layers.IndexOf(layer);

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
            var layers = AppPlayer.active?.appSettings?.layers;

            if(layers == null)
            {
                return null;
            }

            return index >= 0 && index < layers.Count ? layers[index] : null;
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

            var layers = AppPlayer.active?.appSettings?.layers;

            if(layers == null)
            {
                return -1;
            }

            return layers.IndexOf(name);
        }
    }
}
