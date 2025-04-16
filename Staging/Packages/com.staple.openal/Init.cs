using Staple.Internal;
using System;
using System.Collections.Generic;

namespace Staple.OpenALAudio;

public class Init : ModuleInitializer
{
    public override Dictionary<string, Type> GetProvidedTypes()
    {
        return new()
        {
            { nameof(AudioSystem.AudioListenerImpl), typeof(OpenALAudioListener) },
            { nameof(AudioSystem.AudioSourceImpl), typeof(OpenALAudioSource) },
            { nameof(AudioSystem.AudioClipImpl), typeof(OpenALAudioClip) },
            { nameof(AudioSystem.AudioDeviceImpl), typeof(OpenALAudioDevice) },
        };
    }

    public override void InitializeModule()
    {
    }

    public override void CleanupModule()
    {
    }

    public override ModuleType Kind()
    {
        return ModuleType.Audio;
    }
}
