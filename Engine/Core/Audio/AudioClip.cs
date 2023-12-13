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
            var path = AssetDatabase.GetAssetPath(guid);

            if(path == null)
            {
                return null;
            }

            path += ".sbin";

            if (path.EndsWith(".mp3.sbin"))
            {
                try
                {
                    var fileData = ResourceManager.instance.LoadFile(path);

                    if ((fileData?.Length ?? 0) == 0)
                    {
                        Log.Debug($"[AudioSystem] Failed to open audio stream at {path}");

                        return null;
                    }

                    var stream = new MemoryStream(fileData);

                    try
                    {
                        return new MP3AudioStream(stream);
                    }
                    catch (Exception e)
                    {
                        stream.Dispose();

                        Log.Error($"[AudioSystem] Failed to load audio clip for {guid}: {e}");
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"[AudioSystem] Failed to load audio clip for {guid}: {e}");
                }

                return null;
            }
            else if (path.EndsWith(".ogg.sbin"))
            {
                try
                {
                    var fileData = ResourceManager.instance.LoadFile(path);

                    if((fileData?.Length ?? 0) == 0)
                    {
                        Log.Debug($"[AudioSystem] Failed to open audio stream at {path}");

                        return null;
                    }

                    var stream = new MemoryStream(fileData);

                    try
                    {
                        return new OggAudioStream(stream);
                    }
                    catch(Exception e)
                    {
                        stream.Dispose();

                        Log.Error($"[AudioSystem] Failed to load audio clip for {guid}: {e}");
                    }
                }
                catch(Exception e)
                {
                    Log.Error($"[AudioSystem] Failed to load audio clip for {guid}: {e}");
                }

                return null;
            }
            else if(path.EndsWith(".wav.sbin"))
            {
                try
                {
                    var fileData = ResourceManager.instance.LoadFile(path);

                    if ((fileData?.Length ?? 0) == 0)
                    {
                        Log.Debug($"[AudioSystem] Failed to open audio stream at {path}");

                        return null;
                    }

                    var stream = new MemoryStream(fileData);

                    try
                    {
                        return new WaveAudioStream(stream);
                    }
                    catch (Exception e)
                    {
                        stream.Dispose();

                        Log.Error($"[AudioSystem] Failed to load audio clip for {guid}: {e}");
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"[AudioSystem] Failed to load audio clip for {guid}: {e}");
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
