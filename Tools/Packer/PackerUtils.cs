using MessagePack;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;

namespace Packer;

static class PackerUtils
{
    public static string ExtractGuid(string path)
    {
        if(path.EndsWith(".mat"))
        {
            try
            {
                var data = File.ReadAllBytes(path);

                using var stream = new MemoryStream(data);

                var header = MessagePackSerializer.Deserialize<SerializableMaterialHeader>(stream);

                if(header.header.SequenceEqual(SerializableMaterialHeader.ValidHeader) == false ||
                    header.version != SerializableMaterialHeader.ValidVersion)
                {
                    return null;
                }

                var value = MessagePackSerializer.Deserialize<SerializableMaterial>(stream);

                return value.metadata.guid;
            }
            catch(Exception e)
            {
                Console.WriteLine($"Failed to get the guid for {path}: {e}");

                return null;
            }
        }
        else if(path.EndsWith(".stsc"))
        {
            try
            {
                var data = File.ReadAllBytes(path);

                using var stream = new MemoryStream(data);

                var header = MessagePackSerializer.Deserialize<SerializableSceneHeader>(stream);

                if (header.header.SequenceEqual(SerializableSceneHeader.ValidHeader) == false ||
                    header.version != SerializableSceneHeader.ValidVersion)
                {
                    return null;
                }

                var value = MessagePackSerializer.Deserialize<SerializableScene>(stream);

                return value.guid;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to get the guid for {path}: {e}");

                return null;
            }
        }
        else if(path.EndsWith(".stsh"))
        {
            try
            {
                var data = File.ReadAllBytes(path);

                using var stream = new MemoryStream(data);

                var header = MessagePackSerializer.Deserialize<SerializableShaderHeader>(stream);

                if (header.header.SequenceEqual(SerializableShaderHeader.ValidHeader) == false ||
                    header.version != SerializableShaderHeader.ValidVersion)
                {
                    return null;
                }

                var value = MessagePackSerializer.Deserialize<SerializableShader>(stream);

                return value.metadata.guid;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to get the guid for {path}: {e}");

                return null;
            }
        }
        else if(path.EndsWith(".asset"))
        {
            try
            {
                var data = File.ReadAllBytes(path);

                using var stream = new MemoryStream(data);

                var header = MessagePackSerializer.Deserialize<SerializableStapleAssetHeader>(stream);

                if (header.header.SequenceEqual(SerializableStapleAssetHeader.ValidHeader) == false ||
                    header.version != SerializableStapleAssetHeader.ValidVersion)
                {
                    return null;
                }

                var value = MessagePackSerializer.Deserialize<SerializableStapleAsset>(stream);

                return value.guid;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to get the guid for {path}: {e}");

                return null;
            }
        }
        else if(AssetSerialization.TextureExtensions.Any(x => path.EndsWith($".{x}")))
        {
            try
            {
                var data = File.ReadAllBytes(path);

                using var stream = new MemoryStream(data);

                var header = MessagePackSerializer.Deserialize<SerializableTextureHeader>(stream);

                if (header.header.SequenceEqual(SerializableTextureHeader.ValidHeader) == false ||
                    header.version != SerializableTextureHeader.ValidVersion)
                {
                    return null;
                }

                var value = MessagePackSerializer.Deserialize<SerializableTexture>(stream);

                return value.metadata.guid;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to get the guid for {path}: {e}");

                return null;
            }
        }
        else if (AssetSerialization.AudioExtensions.Any(x => path.EndsWith($".{x}")))
        {
            try
            {
                var data = File.ReadAllBytes(path);

                using var stream = new MemoryStream(data);

                var header = MessagePackSerializer.Deserialize<SerializableAudioClipHeader>(stream);

                if (header.header.SequenceEqual(SerializableAudioClipHeader.ValidHeader) == false ||
                    header.version != SerializableAudioClipHeader.ValidVersion)
                {
                    return null;
                }

                var value = MessagePackSerializer.Deserialize<SerializableAudioClip>(stream);

                return value.metadata.guid;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to get the guid for {path}: {e}");

                return null;
            }
        }
        else if (AssetSerialization.MeshExtensions.Any(x => path.EndsWith($".{x}")))
        {
            try
            {
                var data = File.ReadAllBytes(path);

                using var stream = new MemoryStream(data);

                var header = MessagePackSerializer.Deserialize<SerializableMeshAssetHeader>(stream);

                if (header.header.SequenceEqual(SerializableMeshAssetHeader.ValidHeader) == false ||
                    header.version != SerializableMeshAssetHeader.ValidVersion)
                {
                    return null;
                }

                var value = MessagePackSerializer.Deserialize<SerializableMeshAsset>(stream);

                return value.metadata.guid;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to get the guid for {path}: {e}");

                return null;
            }
        }
        else
        {
            return null;
        }
    }
}
