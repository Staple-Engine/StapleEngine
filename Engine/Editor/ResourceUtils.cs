using MessagePack;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;

namespace Staple.Editor
{
    internal static class ResourceUtils
    {
        public static Texture LoadTexture(string path)
        {
            var data = Array.Empty<byte>();

            try
            {
                data = File.ReadAllBytes(path);
            }
            catch(Exception)
            {
                return null;
            }

            using var stream = new MemoryStream(data);

            try
            {
                var header = MessagePackSerializer.Deserialize<SerializableTextureHeader>(stream);

                if (header == null ||
                    header.header.SequenceEqual(SerializableTextureHeader.ValidHeader) == false ||
                    header.version != SerializableTextureHeader.ValidVersion)
                {
                    return null;
                }

                var textureData = MessagePackSerializer.Deserialize<SerializableTexture>(stream);

                if (textureData == null)
                {
                    return null;
                }

                var texture = Texture.Create(path, textureData.data, textureData.metadata);

                if (texture == null)
                {
                    return null;
                }

                return texture;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
