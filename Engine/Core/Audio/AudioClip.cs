using Staple.Internal;
using System;
using System.IO;

namespace Staple
{
    public sealed class AudioClip : IPathAsset
    {
        public AudioClipMetadata metadata;

        private string path;

        public string Path { get => path; set => path = value; }

        internal IAudioStream GetAudioStream()
        {
            if(path.EndsWith(".ogg"))
            {
                try
                {
                    var fileData = ResourceManager.instance.LoadFile($"{path}.sbin");

                    if((fileData?.Length ?? 0) == 0)
                    {
                        return null;
                    }

                    var stream = new MemoryStream(fileData);

                    try
                    {
                        return new OggAudioStream(stream);
                    }
                    catch(Exception)
                    {
                        stream.Dispose();
                    }
                }
                catch(Exception)
                {
                }

                return null;
            }

            return null;
        }

        public static object Create(string path)
        {
            return ResourceManager.instance.LoadAudioClip(path);
        }
    }
}
