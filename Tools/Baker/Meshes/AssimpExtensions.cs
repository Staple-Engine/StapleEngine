using Silk.NET.Assimp;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Baker
{
    internal static class AssimpExtensions
    {
        public class TextureSlot
        {
            public string path;
            public TextureMapping mapping = TextureMapping.UV;
            public uint uvIndex = 0;
            public float blend = 0.0f;
            public TextureOp textureOp = TextureOp.Add;
            public Staple.Internal.TextureWrap mapModeU = Staple.Internal.TextureWrap.Clamp;
            public Staple.Internal.TextureWrap mapModeV = Staple.Internal.TextureWrap.Clamp;
            public uint flags = 0;
        }

        private static Staple.Internal.TextureWrap GetTextureWrapMode(TextureMapMode mode)
        {
            return mode switch
            {
                TextureMapMode.Clamp => Staple.Internal.TextureWrap.Clamp,
                TextureMapMode.Mirror => Staple.Internal.TextureWrap.Mirror,
                TextureMapMode.Wrap => Staple.Internal.TextureWrap.Repeat,
                _ => Staple.Internal.TextureWrap.Clamp,
            };
        }

        public static bool TryGetName(this Material material, Assimp assimp, out string name)
        {
            unsafe
            {
                var nameString = new AssimpString();

                if (assimp.GetMaterialString(ref material, Assimp.MaterialNameBase, 0, 0, ref nameString) == Return.Success)
                {
                    name = nameString.AsString;

                    return true;
                }
            }

            name = default;

            return false;
        }

        public static bool IsTwoSided(this Material material, Assimp assimp)
        {
            unsafe
            {
                var result = 0.0f;
                uint max = 0;

                if(assimp.GetMaterialFloatArray(ref material, Assimp.MaterialTwosidedBase, 0, 0, ref result, ref max) == Return.Success &&
                    result != 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryGetColor(this Material material, string key, Assimp assimp, out Vector4 color)
        {
            unsafe
            {
                var outValue = Vector4.Zero;

                if(assimp.GetMaterialColor(ref material, key, 0, 0, ref outValue) == Return.Success)
                {
                    color = outValue;

                    return true;
                }
            }

            color = Vector4.Zero;

            return false;
        }

        public static bool TryGetTexture(this Material material, TextureType type, Assimp assimp, out TextureSlot slot)
        {
            unsafe
            {
                var path = new AssimpString();
                var mapping = TextureMapping.UV;
                uint uvIndex = 0;
                var blend = 0.0f;
                var textureOp = TextureOp.Add;
                var mapMode = TextureMapMode.Clamp;
                var mapModeU = 0;
                var mapModeV = 0;
                uint flags = 0;
                uint max = 0;

                if(assimp.GetMaterialTexture(ref material, type, 0, ref path, ref mapping, ref uvIndex, ref blend, ref textureOp,
                    ref mapMode, ref flags) == Return.Success)
                {
                    if(assimp.GetMaterialIntegerArray(ref material, Assimp.MaterialMappingmodeUBase, (uint)type, 0, ref mapModeU, ref max) != Return.Success)
                    {
                        mapModeU = (int)mapMode;
                    }

                    if(assimp.GetMaterialIntegerArray(ref material, Assimp.MaterialMappingmodeVBase, (uint)type, 0, ref mapModeV, ref max) != Return.Success)
                    {
                        mapModeV = (int)mapMode;
                    }

                    slot = new()
                    {
                        blend = blend,
                        uvIndex = uvIndex,
                        textureOp = textureOp,
                        flags = flags,
                        mapModeU = GetTextureWrapMode((TextureMapMode)mapModeU),
                        mapModeV = GetTextureWrapMode((TextureMapMode)mapModeV),
                        mapping = mapping,
                        path = path.AsString,
                    };

                    return true;
                }
            }

            slot = null;

            return false;
        }
    }
}