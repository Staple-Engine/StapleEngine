using Staple.Internal;
using System;
using System.Collections.Generic;

namespace Staple.Player.Windows;

public class Init : ModuleInitializer
{
    public override Dictionary<string, Type> ProvidedTypes => [];

    public override ModuleType Kind => ModuleType.Other;

    public override void InitializeModule()
    {
        if(Platform.IsWindows)
        {
            Platform.platformProvider = new WindowsPlatformProvider();
        }
    }

    public override void CleanupModule()
    {
    }
}
