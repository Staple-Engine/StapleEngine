using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Staple.Editor;

internal partial class PackageManager
{
    public static readonly PackageManager instance = new();

    public static readonly PackageList defaultPackageList = new()
    {
        dependencies = new()
        {
            { "com.staple.openal", "1.0.0" },
            { "com.staple.joltphysics", "1.0.0" },
        }
    };

    private readonly Regex versionRegex = VersionRegex();
    private readonly Regex urlRegex = URLRegex();

    public readonly Dictionary<string, (string, Package)> builtinPackages = [];

    public string basePath;

    public readonly Dictionary<string, (string, Package)> projectPackages = [];

    public readonly PackageLockFile lockFile = new();

    public string LockPath => Path.Combine(basePath, "Settings", "packages-lock.json");

    public string PackagesPath => Path.Combine(basePath, "Settings", "packages.json");

    public string PackagesCacheDirectory => Path.Combine(basePath, "Cache", "Packages");

    public string BuiltinPackagesPath => Path.Combine(EditorUtils.EditorPath.Value, "Packages");

    public static void InitializeProject(string basePath)
    {
        var packageList = defaultPackageList;

        try
        {
            var text = JsonConvert.SerializeObject(packageList, Formatting.Indented, Tooling.Utilities.JsonSettings);

            File.WriteAllText(Path.Combine(basePath, "Settings", "packages.json"), text);
        }
        catch(Exception)
        {
        }
    }

    public bool Refresh()
    {
        EditorUtils.CreateDirectory(PackagesCacheDirectory);

        builtinPackages.Clear();
        projectPackages.Clear();

        try
        {
            var directories = Directory.GetDirectories(BuiltinPackagesPath);

            foreach (var d in directories)
            {
                try
                {
                    var package = JsonConvert.DeserializeObject<Package>(File.ReadAllText(Path.Combine(d, "package.json")), Tooling.Utilities.JsonSettings);

                    if(package != null)
                    {
                        builtinPackages.AddOrSetKey(package.name, (d, package));
                    }
                }
                catch(Exception)
                {
                }
            }
        }
        catch(Exception)
        {
        }

        var packageList = ParsePackages(PackagesPath);

        if(packageList == null)
        {
            InitializeProject(basePath);

            packageList = defaultPackageList;
        }

        var packageLock = ParsePackageLock(LockPath);

        packageLock ??= new();

        var updatedLock = new PackageLockFile();

        var missingDependencies = new Dictionary<string, string>();

        void Handle(Dictionary<string, string> dependencies)
        {
            foreach (var dependency in dependencies)
            {
                if(packageLock.dependencies.TryGetValue(dependency.Key, out var state))
                {
                    if(state.version != dependency.Value)
                    {
                        SetupPackage(dependency.Key, dependency.Value, updatedLock, missingDependencies);
                    }
                    else
                    {
                        updatedLock.dependencies.Add(dependency.Key, state);
                    }
                }
                else
                {
                    SetupPackage(dependency.Key, dependency.Value, updatedLock, missingDependencies);
                }
            }

            if(missingDependencies.Count > 0)
            {
                var d = missingDependencies;

                missingDependencies = [];

                Handle(d);
            }
        }

        Handle(packageList.dependencies);

        foreach(var dependency in updatedLock.dependencies)
        {
            try
            {
                var target = Path.Combine(PackagesCacheDirectory, $"{dependency.Key}@{dependency.Value.version}");

                var package = JsonConvert.DeserializeObject<Package>(File.ReadAllText(Path.Combine(target, "package.json")), Tooling.Utilities.JsonSettings);

                if (package != null)
                {
                    projectPackages.AddOrSetKey(package.name, (target, package));
                }
            }
            catch (Exception)
            {
            }
        }

        //Remove unused packages
        try
        {
            var directories = Directory.GetDirectories(Path.Combine(PackagesCacheDirectory));

            foreach(var directory in directories)
            {
                var name = Path.GetFileName(directory);

                var found = false;

                foreach(var dependency in updatedLock.dependencies)
                {
                    if(name == $"{dependency.Key}@{dependency.Value.version}")
                    {
                        found = true;

                        break;
                    }
                }

                if(found == false)
                {
                    try
                    {
                        Directory.Delete(directory, true);
                    }
                    catch(Exception e)
                    {
                        Log.Error($"[Package Manager] Failed to delete the unused package {name} from the project package cache: {e}");
                    }
                }
            }
        }
        catch(Exception e)
        {
            Log.Error($"[Package Manager] Failed to remove unused packages: {e}");
        }

        lockFile.dependencies = updatedLock.dependencies;

        try
        {
            File.WriteAllText(LockPath, JsonConvert.SerializeObject(updatedLock, Formatting.Indented, Tooling.Utilities.JsonSettings));
        }
        catch(Exception)
        {
        }

        return true;
    }

    private void SetupPackage(string name, string value, PackageLockFile lockFile, Dictionary<string, string> missingDependencies)
    {
        var version = versionRegex.Match(value);

        if(version?.Success ?? false)
        {
            if(builtinPackages.TryGetValue(name, out var p))
            {
                var dependencies = new Dictionary<string, string>();

                foreach(var dependency in p.Item2.dependencies)
                {
                    dependencies.Add(dependency.name, dependency.version);
                }

                lockFile.dependencies.Add(name, new()
                {
                    version = value,
                    source = PackageLockFile.Source.Builtin,
                    dependencies = dependencies,
                });

                var directory = Path.Combine(basePath, "Cache", "Packages", $"{name}@{version}");

                if (Directory.Exists(directory) == false)
                {
                    try
                    {
                        EditorUtils.CopyDirectory(p.Item1, directory);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[Package Manager] Failed to setup a builtin package {name}: {e}");

                        return;
                    }
                }

                //TODO: Dependencies
            }
            else
            {
                //TODO: Get from repo
            }
        }
        else
        {
            var url = urlRegex.Match(value);

            if(url?.Success ?? false)
            {
                //TODO: Git Clone
            }
            else
            {
                //Failed
            }
        }
    }

    public static Package ParsePackage(string path)
    {
        try
        {
            var text = File.ReadAllText(path);

            return JsonConvert.DeserializeObject<Package>(text, Tooling.Utilities.JsonSettings);
        }
        catch(Exception e)
        {
            Log.Error($"[Package Manager] Failed to load the package at {path}: {e}");

            return null;
        }
    }

    public static PackageList ParsePackages(string path)
    {
        try
        {
            var text = File.ReadAllText(path);

            return JsonConvert.DeserializeObject<PackageList>(text, Tooling.Utilities.JsonSettings);
        }
        catch (Exception e)
        {
            Log.Error($"[Package Manager] Failed to load the package list at {path}: {e}");

            return null;
        }
    }

    public static PackageLockFile ParsePackageLock(string path)
    {
        try
        {
            var text = File.ReadAllText(path);

            return JsonConvert.DeserializeObject<PackageLockFile>(text, Tooling.Utilities.JsonSettings);
        }
        catch (Exception e)
        {
            Log.Error($"[Package Manager] Failed to load the package lock at {path}: {e}");

            return null;
        }
    }

    [GeneratedRegex("[0-9]+\\.[0-9]+\\.[0-9]+")]
    private static partial Regex VersionRegex();

    [GeneratedRegex("(\\w+)\\:\\/\\/(\\w+)")]
    private static partial Regex URLRegex();
}
