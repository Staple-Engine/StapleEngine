using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Staple.Editor
{
    partial class StapleEditor
    {
        internal void UpdateCSProj()
        {
            var projectDirectory = Path.Combine(basePath, "Cache", "Assembly");

            try
            {
                Directory.CreateDirectory(projectDirectory);
            }
            catch (Exception)
            {
            }

            GenerateGameCSProj();
            GeneratePlayerCSProj();

            BuildGame();
            LoadGame();
        }

        private void GenerateGameCSProj()
        {
            using var collection = new ProjectCollection();

            var projectDirectory = Path.Combine(basePath, "Cache", "Assembly");
            var assetsDirectory = Path.Combine(basePath, "Assets");

            var projectProperties = new Dictionary<string, string>()
            {
                { "OutputType", "Library" },
                { "TargetFramework", "net7.0" },
                { "StripSymbols", "true" },
                { "PublishAOT", "true" },
                { "IsAOTCompatible", "true" },
                { "AppDesignerFolder", "Properties" },
            };

            var p = new Project(collection);

            p.Xml.Sdk = "Microsoft.NET.Sdk";

            var debugProperty = p.Xml.AddPropertyGroup();

            debugProperty.Condition = " '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ";
            debugProperty.AddProperty("PlatformTarget", "AnyCPU");
            debugProperty.AddProperty("DebugType", "pdbonly");
            debugProperty.AddProperty("DebugSymbols", "true");
            debugProperty.AddProperty("Optimize", "false");
            debugProperty.AddProperty("DefineConstants", "_DEBUG");
            debugProperty.AddProperty("ErrorReport", "prompt");
            debugProperty.AddProperty("WarningLevel", "4");

            var releaseProperty = p.Xml.AddPropertyGroup();

            releaseProperty.Condition = " '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ";
            releaseProperty.AddProperty("PlatformTarget", "AnyCPU");
            releaseProperty.AddProperty("DebugType", "portable");
            releaseProperty.AddProperty("DebugSymbols", "true");
            releaseProperty.AddProperty("Optimize", "true");
            releaseProperty.AddProperty("DefineConstants", "NDEBUG");
            releaseProperty.AddProperty("ErrorReport", "prompt");
            releaseProperty.AddProperty("WarningLevel", "4");

            foreach (var pair in projectProperties)
            {
                p.SetProperty(pair.Key, pair.Value);
            }

            p.AddItem("Reference", "StapleCore", new KeyValuePair<string, string>[] { new("HintPath", Path.Combine(AppContext.BaseDirectory, "StapleCore.dll")) });

            void Recursive(string path)
            {
                try
                {
                    var files = Directory.GetFiles(path, "*.cs");

                    foreach (var file in files)
                    {
                        p.AddItem("Compile", Path.GetFullPath(file));
                    }

                    var directories = Directory.GetDirectories(path);

                    foreach (var directory in directories)
                    {
                        Recursive(directory);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed generating csproj: {e}");
                }
            }

            Recursive(assetsDirectory);

            p.Save(Path.Combine(projectDirectory, "Game.csproj"));
        }

        private void GeneratePlayerCSProj()
        {
            using var collection = new ProjectCollection();

            var projectDirectory = Path.Combine(basePath, "Cache", "Assembly");
            var assetsDirectory = Path.Combine(basePath, "Assets");

            var projectProperties = new Dictionary<string, string>()
            {
                { "OutputType", "Exe" },
                { "TargetFramework", "net7.0" },
                { "StripSymbols", "true" },
                { "PublishAOT", "true" },
                { "IsAOTCompatible", "true" },
                { "AppDesignerFolder", "Properties" },
            };

            var p = new Project(collection);

            p.Xml.Sdk = "Microsoft.NET.Sdk";

            var debugProperty = p.Xml.AddPropertyGroup();

            debugProperty.Condition = " '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ";
            debugProperty.AddProperty("PlatformTarget", "AnyCPU");
            debugProperty.AddProperty("DebugType", "pdbonly");
            debugProperty.AddProperty("DebugSymbols", "true");
            debugProperty.AddProperty("Optimize", "false");
            debugProperty.AddProperty("DefineConstants", "_DEBUG");
            debugProperty.AddProperty("ErrorReport", "prompt");
            debugProperty.AddProperty("WarningLevel", "4");

            var releaseProperty = p.Xml.AddPropertyGroup();

            releaseProperty.Condition = " '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ";
            releaseProperty.AddProperty("PlatformTarget", "AnyCPU");
            releaseProperty.AddProperty("DebugType", "portable");
            releaseProperty.AddProperty("DebugSymbols", "true");
            releaseProperty.AddProperty("Optimize", "true");
            releaseProperty.AddProperty("DefineConstants", "NDEBUG");
            releaseProperty.AddProperty("ErrorReport", "prompt");
            releaseProperty.AddProperty("WarningLevel", "4");

            foreach (var pair in projectProperties)
            {
                p.SetProperty(pair.Key, pair.Value);
            }

            var higherDir = AppContext.BaseDirectory.Split(Path.DirectorySeparatorChar).ToList();

            while (higherDir.Count > 0 && higherDir.LastOrDefault() != "StapleEngine")
            {
                higherDir.RemoveAt(higherDir.Count - 1);
            }

            var typeRegistrationPath = Path.Combine(string.Join(Path.DirectorySeparatorChar, higherDir), "Engine", "TypeRegistration", "TypeRegistration.csproj");

            p.AddItem("Reference", "StapleCore", new KeyValuePair<string, string>[] { new("HintPath", Path.Combine(AppContext.BaseDirectory, "StapleCore.dll")) });
            p.AddItem("ProjectReference", typeRegistrationPath,
                new KeyValuePair<string, string>[] {
                    new("OutputItemType", "Analyzer"),
                    new("ReferenceOutputAssembly", "false")
                });

            var programPath = Path.Combine(StapleBasePath, "Engine", "Player", "Program.cs");

            p.AddItem("Compile", Path.GetFullPath(programPath));

            void Recursive(string path)
            {
                try
                {
                    var files = Directory.GetFiles(path, "*.cs");

                    foreach (var file in files)
                    {
                        p.AddItem("Compile", Path.GetFullPath(file));
                    }

                    var directories = Directory.GetDirectories(path);

                    foreach (var directory in directories)
                    {
                        Recursive(directory);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed generating csproj: {e}");
                }
            }

            Recursive(assetsDirectory);

            p.Save(Path.Combine(projectDirectory, "Player.csproj"));
        }
    }
}
