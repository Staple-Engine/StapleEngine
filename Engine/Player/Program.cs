using System;

namespace Staple
{
    static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"Staple: Registering type cache");

            TypeCacheRegistration.RegisterAll();

            Console.WriteLine($"Staple: Registered {TypeCache.AllTypes().Length} types");

            StaplePlayer.Run(args);
        }
    }
}
