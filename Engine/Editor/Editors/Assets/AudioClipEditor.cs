using Staple.Internal;
using System;
using System.IO;
using System.Threading;

namespace Staple.Editor;

[CustomEditor(typeof(AudioClipMetadata))]
internal class AudioClipEditor : AssetEditor
{
    private AudioClip clip;
    private bool triedLoad = false;
    private IAudioSource audioSource;
    private IAudioClip audioClip;
    private CancellationTokenSource cancellation;
    private readonly Lock lockObject = new();
    private long sizeInDisk;
    private long sizeUncompressed;

    public override bool DrawProperty(Type type, string name, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        if(name == nameof(AudioClipMetadata.recompression) ||
            name == nameof(AudioClipMetadata.recompressionQuality))
        {
            var extension = Path.GetExtension(path.Replace(".meta", "")).ToUpperInvariant();

            var skip = extension != ".WAV";

            if(skip)
            {
                EditorGUI.Label("Audio compression is only available for uncompressed file formats");
            }

            return skip;
        }

        return false;
    }

    public override void Destroy()
    {
        base.Destroy();

        lock (lockObject)
        {
            cancellation?.Cancel();
            audioSource?.Destroy();
            audioClip?.Destroy();

            triedLoad = false;
            clip = null;
            cancellation = null;
            audioSource = null;
            audioClip = null;
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var metadata = (AudioClipMetadata)target;
        var originalMetadata = (AudioClipMetadata)original;

        lock (lockObject)
        {
            if (triedLoad == false && clip == null)
            {
                triedLoad = true;

                try
                {
                    sizeInDisk = new FileInfo(cachePath.Replace(".meta", "")).Length;
                }
                catch(Exception)
                {
                }

                clip = ResourceManager.instance.LoadAudioClip(cachePath);
                cancellation = AudioSystem.Instance.LoadAudioClip(clip, (samples, channels, bits, sampleRate) =>
                {
                    if (samples == default)
                    {
                        return;
                    }

                    lock (lockObject)
                    {
                        audioSource = (IAudioSource)Activator.CreateInstance(AudioSystem.AudioSourceImpl);

                        if (audioSource.Init())
                        {
                            audioClip = (IAudioClip)Activator.CreateInstance(AudioSystem.AudioClipImpl);

                            if (audioClip.Init(samples, channels, bits, sampleRate))
                            {
                                sizeUncompressed = samples.Length * sizeof(ushort);

                                if (audioSource.Bind(audioClip) == false)
                                {
                                    audioClip.Destroy();

                                    audioSource.Destroy();

                                    audioClip = null;
                                    audioSource = null;
                                }
                            }
                            else
                            {
                                audioClip.Destroy();

                                audioSource.Destroy();

                                audioClip = null;
                                audioSource = null;
                            }
                        }
                        else
                        {
                            audioSource = null;
                        }
                    }
                });
            }

            if (clip == null && triedLoad)
            {
                EditorGUI.Label("Audio data is corrupted");

                return;
            }

            var hours = 0;
            var minutes = 0;
            var seconds = clip.duration;

            while (seconds > 60)
            {
                seconds -= 60;

                minutes++;
            }

            while (minutes > 60)
            {
                minutes -= 60;

                hours++;
            }

            EditorGUI.Label($"Channels: {clip.channels} ({clip.bitsPerSample} bits, {clip.sampleRate}Hz)");
            EditorGUI.Label($"Duration: {hours}:{minutes}:{seconds.ToString("0.00")}");
            EditorGUI.Label($"Disk Size: {EditorUtils.ByteSizeString(sizeInDisk)} ({EditorUtils.ByteSizeString(sizeUncompressed)} uncompressed, " +
                $"{sizeInDisk / (float)sizeUncompressed * 100:0.00}%% ratio)");

            if (audioSource != null)
            {
                EditorGUI.Button("Play", "AudioSourcePlay", () =>
                {
                    audioSource.Play();
                });

                EditorGUI.SameLine();

                if (audioSource.Playing)
                {
                    EditorGUI.Button("Pause", "AudioSourcePause", () =>
                    {
                        audioSource.Pause();
                    });
                }
                else if (audioSource.Paused)
                {
                    EditorGUI.Button("Resume", "AudioSourceResume", () =>
                    {
                        audioSource.Play();
                    });
                }
                else
                {
                    EditorGUI.ButtonDisabled("Pause", "AudioSourcePause", null);
                }

                EditorGUI.SameLine();

                if (audioSource.Playing)
                {
                    EditorGUI.Button("Stop", "AudioSourceStop", () =>
                    {
                        audioSource.Stop();
                    });
                }
                else
                {
                    EditorGUI.ButtonDisabled("Stop", "AudioSourceStop", null);
                }
            }

            ShowAssetUI((() =>
            {
                Destroy();
            }));
        }
    }
}
