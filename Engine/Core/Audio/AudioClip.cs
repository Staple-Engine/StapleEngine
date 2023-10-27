using Staple.Internal;
using System;
using System.IO;

namespace Staple
{
    public sealed class AudioClip : IPathAsset
    {
        public AudioClipMetadata metadata;

        [NonSerialized]
        public int sizeInBytes;

        [NonSerialized]
        public float duration;

        [NonSerialized]
        public int channels;

        [NonSerialized]
        public int bitsPerSample;

        [NonSerialized]
        public int sampleRate;

        internal short[] samples;

        private string path;

        public string Path { get => path; set => path = value; }

        internal IAudioStream GetAudioStream()
        {
            if (path.EndsWith(".mp3"))
            {
                try
                {
                    var fileData = ResourceManager.instance.LoadFile($"{path}.sbin");

                    if ((fileData?.Length ?? 0) == 0)
                    {
                        return null;
                    }

                    var stream = new MemoryStream(fileData);

                    try
                    {
                        return new MP3AudioStream(stream);
                    }
                    catch (Exception)
                    {
                        stream.Dispose();
                    }
                }
                catch (Exception)
                {
                }

                return null;
            }
            if (path.EndsWith(".ogg"))
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
            else if(path.EndsWith(".wav"))
            {
                try
                {
                    var fileData = ResourceManager.instance.LoadFile($"{path}.sbin");

                    if ((fileData?.Length ?? 0) == 0)
                    {
                        return null;
                    }

                    var stream = new MemoryStream(fileData);

                    try
                    {
                        return new WaveAudioStream(stream);
                    }
                    catch (Exception)
                    {
                        stream.Dispose();
                    }
                }
                catch (Exception)
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
