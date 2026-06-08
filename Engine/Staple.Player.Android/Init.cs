using Staple.Internal;
using System;
using System.Collections.Generic;

namespace Staple.Player.Android;

public class Init : ModuleInitializer
{
    public override Dictionary<string, Type> ProvidedTypes => [];

    public override ModuleType Kind => ModuleType.Other;

    public override void InitializeModule()
    {
        Platform.platformProvider = AndroidPlatformProvider.Instance;
    }

    public override void CleanupModule()
    {
    }
}
