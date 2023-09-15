using MessagePack;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;

namespace Packer
{
    static class PackerUtils
    {
        private static string[] textureExtensions = new string[]
        {
            "bmp",
            "dds",
            "exr",
            "gif",
            "jpg",
            "jpeg",
            "hdr",
            "ktx",
            "png",
            "psd",
            "pvr",
            "tga"
        };

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

                    var value = MessagePackSerializer.Deserialize<MaterialMetadata>(stream);

                    return value.guid;
                }
                catch(Exception)
                {
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
                catch (Exception)
                {
                    return null;
                }
            }
            else if(textureExtensions.Any(x => path.EndsWith($".{x}")))
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
                catch (Exception)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
