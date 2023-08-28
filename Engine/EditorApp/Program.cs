using Staple;
using Staple.Editor;
using System;

namespace StapleEditorApp
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            TypeCacheRegistration.RegisterAll();

            new StapleEditor().Run();
        }
    }
}