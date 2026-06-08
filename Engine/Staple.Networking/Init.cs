using Staple.Internal;
using System;
using System.Collections.Generic;

namespace Staple.Networking;

public class Init : ModuleInitializer
{
    public override Dictionary<string, Type> ProvidedTypes => [];

    public override ModuleType Kind => ModuleType.Other;

    public override void InitializeModule()
    {
    }

    public override void CleanupModule()
    {
    }
}
