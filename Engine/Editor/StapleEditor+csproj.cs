using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;

namespace Staple.Editor
{
    internal partial class StapleEditor
    {
        private static Dictionary<AppPlatform, string[]> platformDefines = new()
        {
            { AppPlatform.Windows, new string[]{ "STAPLE_ENGINE", "STAPLE_WINDOWS" } },
            { AppPlatform.Linux, new string[]{ "STAPLE_ENGINE", "STAPLE_LINUX" } },
            { AppPlatform.MacOSX, new string[]{ "STAPLE_ENGINE", "STAPLE_MACOSX" } },
            { AppPlatform.Android, new string[]{ "STAPLE_ENGINE", "STAPLE_ANDROID" } },
            { AppPlatform.iOS, new string[]{ "STAPLE_ENGINE", "STAPLE_IOS" } },
        };

        internal void UpdateCSProj(AppPlatform platform)
        {
            var projectDirectory = Path.Combine(basePath, "Cache", "Assembly");

            try
            {
                Directory.CreateDirectory(projectDirectory);
            }
            catch (Exception)
            {
            }

            GenerateGameCSProj(platform);

            BuildGame();
            LoadGame();
        }

        private void GenerateGameCSProj(AppPlatform platform)
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

            var platformDefinesString = "";

            if (platformDefines.TryGetValue(platform, out var defines) && defines.Length > 0)
            {
                platformDefinesString = $";{string.Join(";", defines)}";
            }

            var p = new Project(collection);

            p.Xml.Sdk = "Microsoft.NET.Sdk";

            var debugProperty = p.Xml.AddPropertyGroup();

            debugProperty.Condition = " '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ";
            debugProperty.AddProperty("PlatformTarget", "AnyCPU");
            debugProperty.AddProperty("DebugType", "pdbonly");
            debugProperty.AddProperty("DebugSymbols", "true");
            debugProperty.AddProperty("Optimize", "false");
            debugProperty.AddProperty("DefineConstants", $"_DEBUG{platformDefinesString}");
            debugProperty.AddProperty("ErrorReport", "prompt");
            debugProperty.AddProperty("WarningLevel", "4");

            var releaseProperty = p.Xml.AddPropertyGroup();

            releaseProperty.Condition = " '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ";
            releaseProperty.AddProperty("PlatformTarget", "AnyCPU");
            releaseProperty.AddProperty("DebugType", "portable");
            releaseProperty.AddProperty("DebugSymbols", "true");
            releaseProperty.AddProperty("Optimize", "true");
            releaseProperty.AddProperty("DefineConstants", $"NDEBUG{platformDefinesString}");
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

        private void GeneratePlayerCSProj(AppPlatform platform)
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
                { "OptimizationPreference", "Speed" },
            };

            var platformDefinesString = "";

            if(platformDefines.TryGetValue(platform, out var defines) && defines.Length > 0)
            {
                platformDefinesString = $";{string.Join(";", defines)}";
            }

            var p = new Project(collection);

            p.Xml.Sdk = "Microsoft.NET.Sdk";

            var debugProperty = p.Xml.AddPropertyGroup();

            debugProperty.Condition = " '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ";
            debugProperty.AddProperty("PlatformTarget", "AnyCPU");
            debugProperty.AddProperty("DebugType", "pdbonly");
            debugProperty.AddProperty("DebugSymbols", "true");
            debugProperty.AddProperty("Optimize", "false");
            debugProperty.AddProperty("DefineConstants", $"_DEBUG{platformDefinesString}");
            debugProperty.AddProperty("ErrorReport", "prompt");
            debugProperty.AddProperty("WarningLevel", "4");

            var releaseProperty = p.Xml.AddPropertyGroup();

            releaseProperty.Condition = " '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ";
            releaseProperty.AddProperty("PlatformTarget", "AnyCPU");
            releaseProperty.AddProperty("DebugType", "portable");
            releaseProperty.AddProperty("DebugSymbols", "true");
            releaseProperty.AddProperty("Optimize", "true");
            releaseProperty.AddProperty("DefineConstants", $"NDEBUG{platformDefinesString}");
            releaseProperty.AddProperty("ErrorReport", "prompt");
            releaseProperty.AddProperty("WarningLevel", "4");

            foreach (var pair in projectProperties)
            {
                p.SetProperty(pair.Key, pair.Value);
            }

            var typeRegistrationPath = Path.Combine(StapleBasePath, "Engine", "TypeRegistration", "TypeRegistration.csproj");

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
