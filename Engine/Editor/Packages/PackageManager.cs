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

    public string PathForPackage(string name, string version)
    {
        return Path.Combine(basePath, "Cache", "Packages", $"{name}@{version}");
    }

    public void AddPackage(string name, string version)
    {
        var packageList = ParsePackages(PackagesPath);

        if(packageList.dependencies.ContainsKey(name))
        {
            return;
        }

        packageList.dependencies.Add(name, version);

        try
        {
            var text = JsonConvert.SerializeObject(packageList, Formatting.Indented, Tooling.Utilities.JsonSettings);

            File.WriteAllText(PackagesPath, text);
        }
        catch (Exception)
        {
        }

        Refresh();

        EditorUtils.RefreshAssets(true, null);
    }

    public void RemovePackage(string name)
    {
        var packageList = ParsePackages(PackagesPath);

        if (packageList.dependencies.ContainsKey(name) == false)
        {
            return;
        }

        packageList.dependencies.Remove(name);

        try
        {
            var text = JsonConvert.SerializeObject(packageList, Formatting.Indented, Tooling.Utilities.JsonSettings);

            File.WriteAllText(PackagesPath, text);
        }
        catch (Exception)
        {
        }

        Refresh();

        EditorUtils.RefreshAssets(true, null);
    }

    public void InstallLocalPackage(string path)
    {
        var package = ParsePackage(path);
        var packageList = ParsePackages(PackagesPath);

        if(package == null ||
            packageList == null)
        {
            return;
        }

        if(packageList.dependencies.ContainsKey(package.name))
        {
            return;
        }

        try
        {
            var target = Path.Combine(PackagesCacheDirectory, $"{package.name}@{package.version}");

            if(EditorUtils.CopyDirectory(Path.GetDirectoryName(path), target) == false)
            {
                Directory.Delete(target, true);

                return;
            }
        }
        catch(Exception)
        {
            return;
        }

        var dependencies = new Dictionary<string, string>();

        foreach(var d in package.dependencies)
        {
            dependencies.AddOrSetKey(d.name, d.version);
        }

        packageList.dependencies.Add(package.name, package.version);

        lockFile.dependencies.Add(package.name, new()
        {
            source = PackageLockFile.Source.Local,
            version = package.version,
            dependencies = dependencies,
        });

        SavePackages(packageList);
        SavePackageLock(lockFile);

        Refresh();

        EditorUtils.RefreshAssets(true, null);
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
                SetupPackage(dependency.Key, dependency.Value, packageList, lockFile, updatedLock, missingDependencies);
            }

            if(missingDependencies.Count > 0)
            {
                var d = missingDependencies;

                missingDependencies = [];

                Handle(d);
            }
        }

        Handle(packageList.dependencies);

        //Load package files and remove invalid/missing ones
        var removed = new List<string>();

        foreach(var dependency in updatedLock.dependencies)
        {
            var target = Path.Combine(PackagesCacheDirectory, $"{dependency.Key}@{dependency.Value.version}");

            try
            {
                var package = JsonConvert.DeserializeObject<Package>(File.ReadAllText(Path.Combine(target, "package.json")), Tooling.Utilities.JsonSettings);

                if (package != null)
                {
                    projectPackages.AddOrSetKey(package.name, (target, package));
                }
            }
            catch (Exception)
            {
                removed.Add(dependency.Key);
            }
        }

        foreach(var r in removed)
        {
            packageList.dependencies.Remove(r);
            updatedLock.dependencies.Remove(r);
        }

        //Remove leftover unused packages
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

        SavePackages(packageList);
        SavePackageLock(updatedLock);

        return true;
    }

    private void SetupPackage(string name, string value, PackageList packageList, PackageLockFile lockFile, PackageLockFile updatedLockFile,
        Dictionary<string, string> missingDependencies)
    {
        var version = versionRegex.Match(value);

        if(version?.Success ?? false)
        {
            value = version.Value;

            if(builtinPackages.TryGetValue(name, out var p))
            {
                var shouldOverwrite = false;
                var oldVersion = value;

                if(p.Item2.version != value)
                {
                    shouldOverwrite = true;

                    value = p.Item2.version;
                }

                var dependencies = new Dictionary<string, string>();

                foreach(var dependency in p.Item2.dependencies)
                {
                    dependencies.Add(dependency.name, dependency.version);
                }

                if(updatedLockFile.dependencies.ContainsKey(name) == false)
                {
                    updatedLockFile.dependencies.Add(name, new()
                    {
                        version = value,
                        source = PackageLockFile.Source.Builtin,
                        dependencies = dependencies,
                    });
                }

                var directory = PathForPackage(name, value);

                if(shouldOverwrite)
                {
                    var oldDirectory = PathForPackage(name, oldVersion);

                    if(Directory.Exists(oldDirectory))
                    {
                        try
                        {
                            Directory.Delete(oldDirectory, true);
                        }
                        catch(Exception)
                        {
                        }
                    }
                }

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

                foreach(var pair in dependencies)
                {
                    if(packageList.dependencies.TryGetValue(pair.Key, out var v))
                    {
                        missingDependencies.Add(pair.Key, v);
                    }
                    else if(builtinPackages.TryGetValue(pair.Key, out var b))
                    {
                        missingDependencies.Add(pair.Key, b.Item2.version);
                    }
                    else
                    {
                        //TOOD: Check repos
                    }
                }
            }
            else
            {
                if(lockFile.dependencies.TryGetValue(name, out var state))
                {
                    if(state.version == version.Value &&
                        Directory.Exists(PathForPackage(name, state.version)))
                    {
                        updatedLockFile.dependencies.Add(name, state);

                        return;
                    }
                }

                //TODO: Get from repo

                packageList.dependencies.Remove(name);
                lockFile.dependencies.Remove(name);
            }
        }
        else
        {
            packageList.dependencies.Remove(name);
            lockFile.dependencies.Remove(name);

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

    public void SavePackages(PackageList packageList)
    {
        try
        {
            var text = JsonConvert.SerializeObject(packageList, Formatting.Indented, Tooling.Utilities.JsonSettings);

            File.WriteAllText(PackagesPath, text);
        }
        catch (Exception e)
        {
            Log.Error($"[Package Manager] Failed to save the package list: {e}");
        }
    }

    public void SavePackageLock(PackageLockFile lockFile)
    {
        try
        {
            var text = JsonConvert.SerializeObject(lockFile, Formatting.Indented, Tooling.Utilities.JsonSettings);

            File.WriteAllText(LockPath, text);
        }
        catch (Exception e)
        {
            Log.Error($"[Package Manager] Failed to save the package lock file: {e}");
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
