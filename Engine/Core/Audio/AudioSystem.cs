using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Threading;

namespace Staple.Internal;

/// <summary>
/// Audio system subsystem
/// </summary>
public class AudioSystem : ISubsystem
{
    private class AudioSourceInfo
    {
        public WeakReference<AudioSource> source;
        public Entity entity;
        public AudioClip clip;
        public float volume;
        public float pitch;
        public bool loop;
        public bool autoplay;
        public IAudioClip activeClip;
        public bool pausedBackground;
    }

    public delegate void AudioClipLoadHandler(short[] samples, int channels, int bitsPerSample, int sampleRate);

    public SubsystemType type => SubsystemType.Update;

    /// <summary>
    /// What implementation to use for the audio device
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public static Type AudioDeviceImpl { get; internal set; }

    /// <summary>
    /// What implementation to use for the audio listener
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public static Type AudioListenerImpl { get; internal set; }

    /// <summary>
    /// What implementation to use for the audio source
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public static Type AudioSourceImpl { get; internal set; }

    /// <summary>
    /// What implementation to use for the audio clip
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public static Type AudioClipImpl { get; internal set; }

    /// <summary>
    /// Internal audio device instance
    /// </summary>
    internal IAudioDevice device;

    internal static readonly byte Priority = 3;

    /// <summary>
    /// Thread to load audio in the background
    /// </summary>
    private Thread backgroundLoadThread;

    /// <summary>
    /// Whether we're shutting down
    /// </summary>
    private bool shuttingDown = false;

    /// <summary>
    /// Thread lock for background usage
    /// </summary>
    private object backgroundLock = new();

    /// <summary>
    /// Pending actions for the background thrread
    /// </summary>
    private readonly Queue<Action> backgroundActions = new();

    /// <summary>
    /// The instance of the audio system
    /// </summary>
    public static readonly AudioSystem Instance = new();

    /// <summary>
    /// All our audio sources
    /// </summary>
    private readonly List<AudioSourceInfo> audioSources = [];

    private SceneQuery<Transform, AudioListener> audioListeners;

    public void Startup()
    {
        if(AudioDeviceImpl == null ||
            AudioDeviceImpl.IsAssignableTo(typeof(IAudioDevice)) == false ||
            AudioListenerImpl == null ||
            AudioListenerImpl.IsAssignableTo(typeof(IAudioListener)) == false ||
            AudioSourceImpl == null ||
            AudioSourceImpl.IsAssignableTo(typeof(IAudioSource)) == false ||
            AudioClipImpl == null ||
            AudioClipImpl.IsAssignableTo(typeof(IAudioClip)) == false)
        {
            return;
        }

        device = ObjectCreation.CreateObject<IAudioDevice>(AudioDeviceImpl);

        if(device != null && device.Init() == false)
        {
            device = null;
        }

        Log.Debug(device != null ? "[AudioSystem] Initialized audio device" : "[AudioSystem] Failed to initialize audio device");

        if(device == null)
        {
            return;
        }

        World.AddComponentAddedCallback(typeof(AudioListener), (World world, Entity entity, ref IComponent component) =>
        {
            if(Platform.IsPlaying == false || entity.TryGetComponent<Transform>(out var transform) == false)
            {
                return;
            }

            var listener = component as AudioListener;

            listener.audioListener = ObjectCreation.CreateObject<IAudioListener>(AudioListenerImpl);

            if(listener.audioListener != null)
            {
                listener.audioListener.Position = transform.Position;
                listener.audioListener.Orientation = transform.Rotation;
            }
        });

        World.AddComponentAddedCallback(typeof(AudioSource), (World world, Entity entity, ref IComponent component) =>
        {
            if (Platform.IsPlaying == false)
            {
                return;
            }

            var source = component as AudioSource;

            source.audioSource = ObjectCreation.CreateObject<IAudioSource>(AudioSourceImpl);

            if(source.audioSource == null || source.audioSource.Init() == false)
            {
                Log.Debug($"[AudioSystem] Failed to create audio source for entity {entity}");

                source.audioSource = null;

                return;
            }

            audioSources.Add(new AudioSourceInfo()
            {
                source = new WeakReference<AudioSource>(source),
                entity = entity,
            });
        });

        World.AddComponentRemovedCallback(typeof(AudioListener), (World world, Entity entity, ref IComponent component) =>
        {
            var listener = component as AudioListener;

            listener.audioListener = null;
        });

        World.AddComponentRemovedCallback(typeof(AudioSource), (World world, Entity entity, ref IComponent component) =>
        {
            var source = component as AudioSource;

            source.audioSource?.Destroy();

            source.audioSource = null;
        });

        backgroundLoadThread = new(() =>
        {
            for(; ; )
            {
                Action next = null;

                lock(backgroundLock)
                {
                    if(shuttingDown)
                    {
                        return;
                    }

                    if(backgroundActions.Count > 0)
                    {
                        next = backgroundActions.Dequeue();
                    }
                }

                if(next != null)
                {
                    try
                    {
                        next?.Invoke();
                    }
                    catch(Exception e)
                    {
                        Log.Debug($"[AudioSystem] Background thread exception: {e}");
                    }
                }

                Thread.Sleep(25);
            }
        });

        backgroundLoadThread.Priority = ThreadPriority.BelowNormal;
        backgroundLoadThread.Start();
    }

    public void Update()
    {
        if(Platform.IsPlaying == false)
        {
            return;
        }

        if(audioListeners == null)
        {
            audioListeners = new();
        }

        Transform listenerTransform = null;
        
        foreach((Entity _, Transform transform, AudioListener listener) in audioListeners)
        {
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
        }

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

                LoadAudioClip(source.audioClip, (samples, channels, bits, sampleRate) =>
                {
                    if(samples == null || channels == 0 || bits == 0 || sampleRate == 0)
                    {
                        Log.Debug($"[AudioSystem] Failed to load audio clip for {source.audioClip.Guid}");

                        return;
                    }

                    var clip = ObjectCreation.CreateObject<IAudioClip>(AudioClipImpl);

                    if (clip != null && clip.Init(samples, channels, bits, sampleRate))
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

                            if (source.autoplay)
                            {
                                source.audioSource?.Play();
                            }
                        }
                    }
                    else
                    {
                        clip?.Destroy();
                    }
                });
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
                var transform = item.entity.GetComponent<Transform>();

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
        lock(backgroundLock)
        {
            shuttingDown = true;
        }

        for(; ; )
        {
            if(backgroundLoadThread == null || backgroundLoadThread.IsAlive == false)
            {
                break;
            }
        }

        device?.Shutdown();
    }

    public void EnterBackground()
    {
        foreach(var source in audioSources)
        {
            if(source.source.TryGetTarget(out var audioSource) && audioSource.audioSource.Playing)
            {
                source.pausedBackground = true;

                audioSource.audioSource.Pause();
            }
        }
    }

    public void EnterForeground()
    {
        foreach (var source in audioSources)
        {
            if (source.source.TryGetTarget(out var audioSource) && source.pausedBackground)
            {
                source.pausedBackground = false;

                audioSource.audioSource.Play();
            }
        }
    }

    public CancellationTokenSource LoadAudioClip(AudioClip clip, AudioClipLoadHandler onFinish)
    {
        var cts = new CancellationTokenSource();

        if (clip.samples != default)
        {
            onFinish?.Invoke(clip.samples, clip.channels, clip.bitsPerSample, clip.sampleRate);

            return cts;
        }

        var token = cts.Token;

        try
        {
            var stream = clip.GetAudioStream();

            if(stream == null)
            {
                Log.Debug($"[AudioSystem] Failed to get audio stream for {clip.Guid}");

                onFinish?.Invoke(default, 0, 0, 0);

                return cts;
            }

            void Finish()
            {
                if(token.IsCancellationRequested)
                {
                    onFinish?.Invoke(default, 0, 0, 0);

                    return;
                }

                var samples = stream.ReadAll();

                clip.sizeInBytes = samples.Length * sizeof(short);
                clip.samples = samples;

                clip.duration = (float)stream.TotalTime.TotalSeconds;
                clip.channels = stream.Channels;
                clip.bitsPerSample = stream.BitsPerSample;
                clip.sampleRate = stream.SampleRate;

                onFinish?.Invoke(samples, stream.Channels, stream.BitsPerSample, stream.SampleRate);
            }

            if(clip.metadata.loadInBackground)
            {
                lock (backgroundLock)
                {
                    backgroundActions.Enqueue(Finish);
                }
            }
            else
            {
                Finish();
            }
        }
        catch (Exception e)
        {
            Log.Debug($"[AudioClip] Failed to load audio clip {clip.Guid}: {e}");
        }

        return cts;
    }
}
