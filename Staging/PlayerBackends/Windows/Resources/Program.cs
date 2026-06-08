using System;
using Staple.Internal;

namespace Staple;

static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine($"Staple: Registering type cache");

        StapleCodeGeneration.TypeCacheRegistration.RegisterAll();

        TypeCache.Freeze();

        Console.WriteLine($"Staple: Registered {TypeCache.AllTypes().Length} types");

        ModuleInitializer.LoadAll();

        StaplePlayer.Run(args);
    }
}
