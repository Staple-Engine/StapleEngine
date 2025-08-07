using Silk.NET.Assimp;
using System;
using System.Linq;
using System.Numerics;

namespace Staple.Tooling
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

        public static bool TryGetName(this Silk.NET.Assimp.Material material, Assimp assimp, out string name)
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

        public static bool IsTwoSided(this Silk.NET.Assimp.Material material, Assimp assimp)
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

        public static bool TryGetColor(this Silk.NET.Assimp.Material material, string key, Assimp assimp, out Vector4 color)
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

        public static bool TryGetTexture(this Silk.NET.Assimp.Material material, TextureType type, Assimp assimp, out TextureSlot slot)
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

        public static unsafe Silk.NET.Assimp.Material*[] GetMaterials(this Silk.NET.Assimp.Scene scene)
        {
            var outValue = new Silk.NET.Assimp.Material*[(int)scene.MNumMaterials];

            for(var i = 0; i < scene.MNumMaterials; i++)
            {
                outValue[i] = scene.MMaterials[i];
            }

            return outValue;
        }

        public static bool HasBones(this Silk.NET.Assimp.Scene scene)
        {
            unsafe
            {
                for (var i = 0; i < scene.MNumMeshes; i++)
                {
                    if (scene.MMeshes[i]->MNumBones > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static int[] MeshIndices(this Node node)
        {
            var outValue = new int[node.MNumMeshes];

            unsafe
            {
                for (var i = 0; i < outValue.Length; i++)
                {
                    outValue[i] = (int)node.MMeshes[i];
                }
            }

            return outValue;
        }

        public static unsafe Node*[] Children(this Node node)
        {
            var outValue = new Node*[node.MNumChildren];

            for (var i = 0; i < outValue.Length; i++)
            {
                outValue[i] = node.MChildren[i];
            }

            return outValue;
        }

        public static unsafe Animation*[] Animations(this Silk.NET.Assimp.Scene scene)
        {
            var outValue = new Animation*[scene.MNumAnimations];

            for (var i = 0; i < outValue.Length; i++)
            {
                outValue[i] = scene.MAnimations[i];
            }

            return outValue;
        }

        public static unsafe NodeAnim*[] Channels(this Animation animation)
        {
            var outValue = new NodeAnim*[animation.MNumChannels];

            for (var i = 0; i < outValue.Length; i++)
            {
                outValue[i] = animation.MChannels[i];
            }

            return outValue;
        }

        public static unsafe VectorKey[] PositionKeys(this NodeAnim channel)
        {
            var outValue = new VectorKey[channel.MNumPositionKeys];

            for (var i = 0; i < outValue.Length; i++)
            {
                outValue[i] = channel.MPositionKeys[i];
            }

            return outValue;
        }

        public static unsafe VectorKey[] ScaleKeys(this NodeAnim channel)
        {
            var outValue = new VectorKey[channel.MNumScalingKeys];

            for (var i = 0; i < outValue.Length; i++)
            {
                outValue[i] = channel.MScalingKeys[i];
            }

            return outValue;
        }

        public static unsafe QuatKey[] RotationKeys(this NodeAnim channel)
        {
            var outValue = new QuatKey[channel.MNumRotationKeys];

            for (var i = 0; i < outValue.Length; i++)
            {
                outValue[i] = channel.MRotationKeys[i];
            }

            return outValue;
        }

        public static unsafe Silk.NET.Assimp.Mesh*[] GetMeshes(this Silk.NET.Assimp.Scene scene)
        {
            var outValue = new Silk.NET.Assimp.Mesh*[scene.MNumMeshes];

            for (var i = 0; i < outValue.Length; i++)
            {
                outValue[i] = scene.MMeshes[i];
            }

            return outValue;
        }

        public static unsafe Face[] GetFaces(this Silk.NET.Assimp.Mesh mesh)
        {
            var outValue = new Face[mesh.MNumFaces];

            for (var i = 0; i < outValue.Length; i++)
            {
                outValue[i] = mesh.MFaces[i];
            }

            return outValue;
        }

        public static int[] GetIndices(this Face face)
        {
            var outValue = new int[face.MNumIndices];

            unsafe
            {
                for (var i = 0; i < outValue.Length; i++)
                {
                    outValue[i] = (int)face.MIndices[i];
                }
            }

            return outValue;
        }

        public static unsafe bool TryGetColors(this Silk.NET.Assimp.Mesh mesh, int index, out Staple.Internal.Vector4Holder[] colors)
        {
            var source = mesh.MColors[index];

            if(source == null)
            {
                colors = [];

                return false;
            }

            colors = new System.Span<Vector4>(source, (int)mesh.MNumVertices)
                .ToArray()
                .Select(x => new Staple.Internal.Vector4Holder(x))
                .ToArray();

            return true;
        }

        public static unsafe bool TryGetTexCoords(this Silk.NET.Assimp.Mesh mesh, int index, out Staple.Internal.Vector2Holder[] uvs)
        {
            var source = mesh.MTextureCoords[index];

            if (source == null)
            {
                uvs = [];

                return false;
            }

            uvs = new System.Span<Vector3>(source, (int)mesh.MNumVertices)
                .ToArray()
                .Select(x => new Staple.Internal.Vector2Holder(x.X, x.Y))
                .ToArray();

            return true;
        }

        public static unsafe Bone *[] GetBones(this Silk.NET.Assimp.Mesh mesh)
        {
            var outValue = new Bone *[mesh.MNumBones];

            for (var i = 0; i < outValue.Length; i++)
            {
                outValue[i] = mesh.MBones[i];
            }

            return outValue;
        }

        public static unsafe VertexWeight[] GetWeights(this Bone bone)
        {
            var outValue = new VertexWeight[bone.MNumWeights];

            for (var i = 0; i < outValue.Length; i++)
            {
                outValue[i] = bone.MWeights[i];
            }

            return outValue;
        }

        public static bool TryGetValue(this Metadata metadata, string name, out object value)
        {
            unsafe
            {
                for (var i = 0; i < metadata.MNumProperties; i++)
                {
                    if (metadata.MKeys[i].AsString == name)
                    {
                        value = metadata.MValues[i].Value();

                        return true;
                    }
                }
            }

            value = null;

            return false;
        }

        public static bool TryGetValue<T>(this Metadata metadata, string name, out T value)
        {
            if(metadata.TryGetValue(name, out object v) && v is T outValue)
            {
                value = outValue;

                return true;
            }

            value = default;

            return false;
        }

        public static object Value(this MetadataEntry entry)
        {
            unsafe
            {

                switch (entry.MType)
                {
                    case MetadataType.Aivector3D:

                        {
                            var span = new Span<byte>(entry.MData, sizeof(float) * 3);

                            return new Vector3(BitConverter.ToSingle(span.Slice(0, sizeof(float))),
                                BitConverter.ToSingle(span.Slice(sizeof(float), sizeof(float))),
                                BitConverter.ToSingle(span.Slice(sizeof(float) * 2, sizeof(float))));
                        }

                    case MetadataType.Bool:

                        return ((byte*)entry.MData)[0] == 1;

                    case MetadataType.Double:

                        {
                            var span = new Span<byte>(entry.MData, sizeof(double));

                            return BitConverter.ToDouble(span);
                        }

                    case MetadataType.Float:

                        {
                            var span = new Span<byte>(entry.MData, sizeof(float));

                            return BitConverter.ToSingle(span);
                        }

                    case MetadataType.Int32:

                        {
                            var span = new Span<byte>(entry.MData, sizeof(int));

                            return BitConverter.ToInt32(span);
                        }

                    case MetadataType.Int64:

                        {
                            var span = new Span<byte>(entry.MData, sizeof(long));

                            return BitConverter.ToInt64(span);
                        }

                    case MetadataType.Uint32:

                        {
                            var span = new Span<byte>(entry.MData, sizeof(uint));

                            return BitConverter.ToUInt32(span);
                        }

                    case MetadataType.Uint64:

                        {
                            var span = new Span<byte>(entry.MData, sizeof(ulong));

                            return BitConverter.ToUInt64(span);
                        }

                    default:

                        return null;
                }
            }
        }
    }
}
