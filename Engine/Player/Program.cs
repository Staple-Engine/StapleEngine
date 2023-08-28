using System;

namespace Staple
{
    static class Program
    {
        public static void Main(string[] args)
        {
            TypeCacheRegistration.RegisterAll();

            StaplePlayer.Run(args);
        }
    }
}
