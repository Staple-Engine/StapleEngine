using System;
using System.Runtime.InteropServices;
using Bgfx;
using GLFW;

namespace Staple
{
    static class Program
    {
        public static void Main(string[] args)
        {
            new AppPlayer(new AppSettings()
            {
                appName = "Test",
                runInBackground = false,
            }).Run();
        }
    }
}
