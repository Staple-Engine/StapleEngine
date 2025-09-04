using Staple.Internal;
using System;
using System.Collections.Generic;

namespace Staple.Player.Linux;

public class Init : ModuleInitializer
{
    public override Dictionary<string, Type> ProvidedTypes => [];

    public override ModuleType Kind => ModuleType.Other;

    public override void InitializeModule()
    {
        if(Platform.IsLinux)
        {
            Platform.platformProvider = new LinuxPlatformProvider();
        }
    }

    public override void CleanupModule()
    {
    }
}
