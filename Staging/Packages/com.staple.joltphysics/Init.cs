using Staple.Internal;
using System;
using System.Collections.Generic;

namespace Staple.JoltPhysics;

public class Init : ModuleInitializer
{
    public override Dictionary<string, Type> ProvidedTypes => new()
    {
        { nameof(Physics3D.Impl), typeof(JoltPhysics3D) }
    };

    public override ModuleType Kind => ModuleType.Physics;

    public override void InitializeModule()
    {
        return;
    }

    public override void CleanupModule()
    {
    }
}
