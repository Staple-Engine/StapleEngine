using Staple.Internal;
using System;
using System.Collections.Generic;

namespace Staple.OpenALAudio;

public class Init : ModuleInitializer
{
    public override Dictionary<string, Type> ProvidedTypes => new()
    {
        { nameof(AudioSystem.AudioListenerImpl), typeof(OpenALAudioListener) },
        { nameof(AudioSystem.AudioSourceImpl), typeof(OpenALAudioSource) },
        { nameof(AudioSystem.AudioClipImpl), typeof(OpenALAudioClip) },
        { nameof(AudioSystem.AudioDeviceImpl), typeof(OpenALAudioDevice) },
    };

    public override ModuleType Kind => ModuleType.Audio;

    public override void InitializeModule()
    {
    }

    public override void CleanupModule()
    {
    }
}
