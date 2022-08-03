using MessagePack;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Staple.Internal
{
    internal class ResourceLocator
    {
        public string basePath;

        public static ResourceLocator instance = new ResourceLocator();

        private Dictionary<string, Texture> cachedTextures = new Dictionary<string, Texture>();
        
        internal void Destroy()
        {
            foreach(var pair in cachedTextures)
            {
                if(pair.Value != null)
                {
                    pair.Value.Destroy();
                }
            }
        }

        public byte[] LoadFile(string path)
        {
            if(basePath == null)
            {
                return null;
            }

            try
            {
                return File.ReadAllBytes(Path.Combine(basePath, path));
            }
            catch(Exception)
            {
                return null;
            }
        }

        public Texture LoadTexture(string path, TextureFlags flags = TextureFlags.None, byte skip = 0)
        {
            if(cachedTextures.TryGetValue(path, out var texture) && texture != null)
            {
                return texture;
            }

            var data = LoadFile(path);

            if(data == null)
            {
                return null;
            }

            using(var stream = new MemoryStream(data))
            {
                try
                {
                    var header = MessagePackSerializer.Deserialize<SerializableTextureHeader>(stream);

                    if(header == null || header.header.SequenceEqual(SerializableTextureHeader.ValidHeader) == false || header.version != SerializableTextureHeader.ValidVersion)
                    {
                        return null;
                    }

                    var textureData = MessagePackSerializer.Deserialize<SerializableTexture>(stream);

                    if(textureData == null)
                    {
                        return null;
                    }

                    texture = Texture.Create(textureData.data, textureData.metadata, flags, skip);

                    if (texture == null)
                    {
                        return null;
                    }

                    if (cachedTextures.ContainsKey(path))
                    {
                        cachedTextures[path] = texture;
                    }
                    else
                    {
                        cachedTextures.Add(path, texture);
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
}
