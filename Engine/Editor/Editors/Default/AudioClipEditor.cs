using Staple.Internal;
using System;

namespace Staple.Editor
{
    [CustomEditor(typeof(AudioClipMetadata))]
    internal class AudioClipEditor : Editor
    {
        private AudioClip clip;
        private IAudioStream stream;
        private bool triedLoad = false;
        private IAudioSource audioSource;
        private IAudioClip audioClip;

        ~AudioClipEditor()
        {
            clip = null;
            audioClip = null;

            stream?.Close();
            audioSource?.Destroy();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if(triedLoad == false && clip == null)
            {
                triedLoad = true;

                clip = ResourceManager.instance.LoadAudioClip(cachePath);

                if(clip != null)
                {
                    stream = clip.GetAudioStream();
                }

                if(stream != null)
                {
                    audioSource = (IAudioSource)Activator.CreateInstance(AudioSystem.AudioSourceType);

                    if(audioSource.Init())
                    {
                        audioClip = (IAudioClip)Activator.CreateInstance(AudioSystem.AudioClipType);

                        if(audioClip.Init(stream.ReadAll(), stream.Channels, stream.BitsPerSample, stream.SampleRate))
                        {
                            if(audioSource.Bind(audioClip) == false)
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
            }
            
            if(stream == null && triedLoad)
            {
                EditorGUI.Label("Audio data is corrupted");

                return;
            }

            var hours = 0;
            var minutes = 0;
            var seconds = stream.TotalTime.TotalSeconds;

            while(seconds > 60)
            {
                seconds -= 60;

                minutes++;
            }

            while(minutes > 60)
            {
                minutes -= 60;

                hours++;
            }

            EditorGUI.Label($"Channels: {stream.Channels} ({stream.BitsPerSample} bits)");
            EditorGUI.Label($"Sample Rate: {stream.SampleRate}");
            EditorGUI.Label($"Duration: {hours}:{minutes}:{seconds.ToString("0.00")}");

            if(audioSource != null)
            {
                if(EditorGUI.Button("Play"))
                {
                    audioSource.Play();
                }

                EditorGUI.SameLine();
                
                if(audioSource.Playing)
                {
                    if(EditorGUI.Button("Pause"))
                    {
                        audioSource.Pause();
                    }
                }
                else if(audioSource.Paused)
                {
                    if(EditorGUI.Button("Resume"))
                    {
                        audioSource.Play();
                    }
                }
                else
                {
                    EditorGUI.ButtonDisabled("Pause");
                }

                EditorGUI.SameLine();

                if (audioSource.Playing)
                {
                    if(EditorGUI.Button("Stop"))
                    {
                        audioSource.Stop();
                    }
                }
                else
                {
                    EditorGUI.ButtonDisabled("Stop");
                }
            }
        }
    }
}
