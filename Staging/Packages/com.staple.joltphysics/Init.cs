using Staple.Internal;
using System;
using System.Collections.Generic;

namespace Staple.JoltPhysics;

public class Init : ModuleInitializer
{
    public override void InitializeModule()
    {
    }

    public override void CleanupModule()
    {
    }

    public override Dictionary<string, Type> GetProvidedTypes()
    {
        return new()
        {
            { nameof(Physics3D.Impl), typeof(JoltPhysics3D) }
        };
    }

    public override ModuleType Kind()
    {
        return ModuleType.Physics;
    }
}
