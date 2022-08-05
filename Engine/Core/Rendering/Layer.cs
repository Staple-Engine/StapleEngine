using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public struct LayerMask
    {
        public uint value;

        internal bool HasLayer(uint index)
        {
            return (value & (1 << (int)index)) == (1 << (int)index);
        }

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
                if(layers.TryGetValue(layer, out var index))
                {
                    mask |= (uint)(1 << (int)index);
                }
            }

            return mask;
        }

        public static string LayerToName(int index)
        {
            var layerPairs = AppPlayer.active?.appSettings?.layers?.Keys.ToList() ?? new List<string>();

            return index < layerPairs.Count ? layerPairs[index] : null;
        }

        public static int NameToLayer(string name)
        {
            uint value = 0;

            return (AppPlayer.active?.appSettings?.layers?.TryGetValue(name, out value) ?? false) ? (int)value : -1;
        }
    }
}
