using Staple.Internal;
using System;
using System.Runtime.InteropServices.JavaScript;

// Create a "Main" method. This is required by the tooling.
return;

namespace Staple
{
    static partial class Program
    {
        [JSExport]
        public static void Main(string[] args)
        {
            Console.WriteLine($"Staple: Registering type cache");

            StapleCodeGeneration.TypeCacheRegistration.RegisterAll();

            Console.WriteLine($"Staple: Registered {TypeCache.AllTypes().Length} types");

            StaplePlayer.Run(args);
        }
    }
}
