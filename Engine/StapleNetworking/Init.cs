using Staple.Internal;
using System;
using System.Collections.Generic;

namespace Staple.Networking;

public class Init : ModuleInitializer
{
    public override Dictionary<string, Type> GetProvidedTypes()
    {
        return [];
    }

    public override void InitializeModule()
    {
    }

    public override void CleanupModule()
    {
    }

    public override ModuleType Kind()
    {
        return ModuleType.Other;
    }
}
