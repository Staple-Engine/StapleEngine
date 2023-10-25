using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple
{
    using AudioDeviceImpl = OpenALAudioDevice;
    using AudioListenerImpl = OpenALAudioListener;
    using AudioSourceImpl = OpenALAudioSource;
    using AudioClipImpl = OpenALAudioClip;

    internal class AudioSystem : ISubsystem
    {
        class AudioSourceInfo
        {
            public WeakReference<AudioSource> source;
            public Entity entity;
            public AudioClip clip;
            public float volume;
            public float pitch;
            public bool loop = false;
            public IAudioClip activeClip;
        }

        public SubsystemType type => SubsystemType.Update;

        internal static readonly Type AudioDeviceType = typeof(AudioDeviceImpl);
        internal static readonly Type AudioListenerType = typeof(AudioListenerImpl);
        internal static readonly Type AudioSourceType = typeof(AudioSourceImpl);
        internal static readonly Type AudioClipType = typeof(AudioClipImpl);

        internal IAudioDevice device;

        internal static readonly byte Priority = 3;

        public static readonly AudioSystem Instance = new();

        private readonly List<AudioSourceInfo> audioSources = new();

        public void Startup()
        {
            device = new AudioDeviceImpl();

            if(device.Init() == false)
            {
                device = null;
            }

            Log.Debug(device != null ? "[AudioSystem] Initialized audio device" : "[AudioSystem] Failed to initialize audio device");

            if(device == null)
            {
                return;
            }

            World.AddComponentAddedCallback(typeof(AudioListener), (Entity entity, Transform transform, ref IComponent component) =>
            {
                if(Platform.IsPlaying == false)
                {
                    return;
                }

                var listener = component as AudioListener;

                listener.audioListener = new AudioListenerImpl
                {
                    Position = transform.Position,
                    Orientation = transform.Rotation
                };
            });

            World.AddComponentAddedCallback(typeof(AudioSource), (Entity entity, Transform transform, ref IComponent component) =>
            {
                if (Platform.IsPlaying == false)
                {
                    return;
                }

                var source = component as AudioSource;

                source.audioSource = new AudioSourceImpl();

                if(source.audioSource.Init() == false)
                {
                    Log.Debug($"[AudioSystem] Failed to create audio source for entity {Scene.current.world.GetEntityName(entity)}");

                    source.audioSource = null;

                    return;
                }

                audioSources.Add(new AudioSourceInfo()
                {
                    source = new WeakReference<AudioSource>(source),
                    entity = entity,
                });
            });

            World.AddComponentRemovedCallback(typeof(AudioListener), (Entity entity, Transform transform, ref IComponent component) =>
            {
                var listener = component as AudioListener;

                listener.audioListener = null;
            });

            World.AddComponentRemovedCallback(typeof(AudioSource), (Entity entity, Transform transform, ref IComponent component) =>
            {
                var source = component as AudioSource;

                source.audioSource?.Destroy();

                source.audioSource = null;
            });
        }

        public void Update()
        {
            if(Platform.IsPlaying == false)
            {
                return;
            }

            Transform listenerTransform = null;

            Scene.current?.world?.ForEach((Entity entity, bool enabled, ref Transform transform, ref AudioListener listener) =>
            {
                if (enabled == false)
                {
                    return;
                }

                listenerTransform ??= transform;

                if (listener.spatial)
                {
                    listener.audioListener.Position = transform.Position;
                    listener.audioListener.Orientation = transform.Rotation;
                }
                else
                {
                    listener.audioListener.Position = Vector3.Zero;
                    listener.audioListener.Orientation = Quaternion.Identity;
                }
            });

            var removed = new List<AudioSourceInfo>();

            foreach (var item in audioSources)
            {
                if (item.source.TryGetTarget(out var source) == false)
                {
                    removed.Add(item);

                    continue;
                }

                if (source.audioClip == null)
                {
                    item.activeClip?.Destroy();

                    item.clip = null;
                }

                if (item.clip != source.audioClip && source.audioClip != null)
                {
                    item.clip = source.audioClip;

                    var stream = source.audioClip.GetAudioStream();

                    if (stream != null)
                    {
                        var clip = new AudioClipImpl();

                        if (clip.Init(stream.ReadAll(), stream.Channels, stream.BitsPerSample, stream.SampleRate))
                        {
                            if (source.audioSource.Bind(clip) == false)
                            {
                                clip.Destroy();
                                source.audioSource.Destroy();

                                source.audioSource = null;
                            }
                            else
                            {
                                item.activeClip = clip;
                            }
                        }
                        else
                        {
                            clip.Destroy();
                        }
                    }
                }

                if (source.audioSource != null)
                {
                    if (item.volume != source.volume)
                    {
                        source.audioSource.Volume = source.volume;
                    }

                    if (item.pitch != source.pitch)
                    {
                        source.audioSource.Pitch = source.pitch;
                    }

                    if (item.loop != source.loop)
                    {
                        source.audioSource.Looping = source.loop;
                    }
                }

                item.volume = source.volume;
                item.pitch = source.pitch;
                item.loop = source.loop;

                if (source.spatial)
                {
                    var transform = Scene.current.world.GetComponent<Transform>(item.entity);

                    source.audioSource.Position = transform.Position;
                }
                else
                {
                    source.audioSource.Position = listenerTransform?.Position ?? Vector3.Zero;
                }
            }

            foreach (var item in removed)
            {
                audioSources.Remove(item);
            }
        }

        public void Shutdown()
        {
            device?.Shutdown();
        }
    }
}
