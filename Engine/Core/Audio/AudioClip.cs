using Staple.Internal;
using System;
using System.IO;

namespace Staple
{
    public sealed class AudioClip : IGuidAsset
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

        private string guid;

        public string Guid { get => guid; set => guid = value; }

        internal IAudioStream GetAudioStream()
        {
            if (guid.EndsWith(".mp3"))
            {
                try
                {
                    var fileData = ResourceManager.instance.LoadFile($"{guid}.sbin");

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
            if (guid.EndsWith(".ogg"))
            {
                try
                {
                    var fileData = ResourceManager.instance.LoadFile($"{guid}.sbin");

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
            else if(guid.EndsWith(".wav"))
            {
                try
                {
                    var fileData = ResourceManager.instance.LoadFile($"{guid}.sbin");

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
