using System.Text;
using System;
using System.IO;
using System.Numerics;
using System.Linq;
using Staple.Internal;

namespace Staple.Editor;

public static class EditorUtils
{
    private static readonly string[] byteSizes =
    [
        "B",
        "KB",
        "MB",
        "GB",
        "TB"
    ];

    internal static Lazy<string> EditorPath = new(() =>
    {
        var basePath = Path.Combine(Storage.StapleBasePath, "Staging");

        try
        {
            if (Directory.Exists(basePath) == false)
            {
                basePath = Path.Combine(Storage.StapleBasePath, "Editor");
            }
        }
        catch (Exception)
        {
        }

        return basePath;
    });

    /// <summary>
    /// Attempts to copy a file
    /// </summary>
    /// <param name="source">The source file</param>
    /// <param name="destination">The destination file</param>
    /// <returns>Whether it did so successfully</returns>
    public static bool CopyFile(string source, string destination)
    {
        try
        {
            File.Copy(source, destination, true);
        }
        catch(Exception)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Attempts to write file contents for a file
    /// </summary>
    /// <param name="path">The path to write to</param>
    /// <param name="contents">The contents to write</param>
    /// <returns>Whether it did so successfully</returns>
    public static bool WriteFile(string path, string contents)
    {
        CreateDirectory(Path.GetDirectoryName(path));

        try
        {
            File.WriteAllText(path, contents);
        }
        catch(Exception)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Attempts to write file contents for a file
    /// </summary>
    /// <param name="path">The path to write to</param>
    /// <param name="contents">The contents to write</param>
    /// <returns>Whether it did so successfully</returns>
    public static bool WriteFile(string path, byte[] contents)
    {
        CreateDirectory(Path.GetDirectoryName(path));

        try
        {
            File.WriteAllBytes(path, contents);
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Attempts to create a directory
    /// </summary>
    /// <param name="path">The directory's path</param>
    public static void CreateDirectory(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
        }
        catch(Exception)
        {
        }
    }

    /// <summary>
    /// Attempts to copy a directory and its contents to another location
    /// </summary>
    /// <param name="source">The source path</param>
    /// <param name="destination">The target path</param>
    /// <returns>Whether it did so successfully</returns>
    public static bool CopyDirectory(string source, string destination)
    {
        CreateDirectory(destination);

        try
        {
            var files = Directory.GetFiles(source, "*");

            foreach (var file in files)
            {
                try
                {
                    File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
                }
                catch (Exception)
                {
                    return false;
                }
            }

            var directories = Directory.GetDirectories(source);

            foreach (var directory in directories)
            {
                if(CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory))) == false)
                {
                    return false;
                }
            }
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Calculates the expanded camel case name for a field name
    /// </summary>
    /// <param name="self">The field name</param>
    /// <returns>The expanded camel case name</returns>
    public static string ExpandCamelCaseName(this string self)
    {
        var outString = new StringBuilder();

        var hadUppercase = true;
        var hadLowercase = false;
        var lastLowercaseIndex = 0;

        for (var i = 0; i < self.Length; i++)
        {
            if (char.IsUpper(self[i]))
            {
                if (!hadUppercase && i > 0 && char.IsDigit(self[i - 1]) == false)
                {
                    outString.Append(self.AsSpan(lastLowercaseIndex, i - lastLowercaseIndex));

                    if (outString.Length > 0)
                    {
                        outString.Append(' ');
                    }

                    lastLowercaseIndex = i;
                }

                outString.Append(self[i]);

                hadUppercase = true;

                if(i == self.Length - 1)
                {
                    break;
                }
            }
            else if (char.IsDigit(self[i]))
            {
                if (i > 0 && !char.IsDigit(self[i - 1]) && !char.IsWhiteSpace(self[i - 1]) && hadLowercase)
                {
                    outString.Append(self.AsSpan(lastLowercaseIndex, i - lastLowercaseIndex));

                    if (outString.Length > 0)
                    {
                        outString.Append(' ');
                    }
                }

                outString.Append(self[i]);

                hadUppercase = false;
                lastLowercaseIndex = (i + 1 < self.Length ? i + 1 : i);
            }
            else
            {
                if (hadUppercase)
                {
                    hadUppercase = false;
                    lastLowercaseIndex = i;
                }

                hadLowercase = true;
            }

            if (i == self.Length - 1)
            {
                if(hadLowercase)
                {
                    if (hadUppercase)
                    {
                        outString.Append(self.AsSpan(lastLowercaseIndex, (i + 1) - lastLowercaseIndex));
                    }
                    else
                    {
                        outString.Append(self.AsSpan(lastLowercaseIndex, (i + 1) - lastLowercaseIndex));
                    }
                }
            }
        }

        var result = outString.ToString();

        if(result.Length > 0 && char.IsLower(result[0]))
        {
            result = char.ToUpperInvariant(result[0]) + result.Substring(1);
        }

        return result;
    }

    /// <summary>
    /// Calculates a string with the abbreviated byte size of a byte count (For example: 1KB)
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public static string ByteSizeString(long size)
    {
        double longSize = size;
        var counter = 0;

        while (longSize >= 1024 && counter < byteSizes.Length)
        {
            counter++;

            longSize /= 1024;
        }

        return $"{longSize:0.00}{byteSizes[counter]}";
    }

    /// <summary>
    /// Refreshes the current assets
    /// </summary>
    /// <param name="onFinish">Callback when finished</param>
    public static void RefreshAssets(Action onFinish)
    {
        RefreshAssets(true, onFinish);
    }

    /// <summary>
    /// Refreshes the current assets
    /// </summary>
    /// <param name="updateProject">Whether to recompile the project</param>
    /// <param name="onFinish">Callback when finished</param>
    internal static void RefreshAssets(bool updateProject, Action onFinish)
    {
        var editor = StapleEditor.instance;

        editor.RefreshAssets(updateProject, () =>
        {
            ThumbnailCache.Clear();

            onFinish?.Invoke();
        });
    }

    public static void ShowMessageBox(string message, string buttonTitle, Action callback)
    {
        StapleEditor.instance.ShowMessageBox(message, buttonTitle, callback);
    }

    public static void ShowMessageBox(string message, string yesTitle, string noTitle, Action onYes, Action onNo)
    {
        StapleEditor.instance.ShowMessageBox(message, yesTitle, noTitle, onYes, onNo);
    }

    internal static Entity InstanceMesh(string name, MeshAsset asset, Entity parentEntity = default)
    {
        Transform parent = null;

        if (parentEntity.IsValid)
        {
            parent = parentEntity.GetComponent<Transform>();
        }

        var baseEntity = Entity.Create(name, typeof(Transform));
        var baseTransform = baseEntity.GetComponent<Transform>();

        baseTransform.SetParent(parent);

        if ((asset.nodes?.Length ?? 0) > 0)
        {
            var parents = new Transform[asset.nodes.Length];

            for (var i = 0; i < asset.nodes.Length; i++)
            {
                var node = asset.nodes[i];

                var nodeParent = Entity.Create(node.name, typeof(Transform));

                var nodeTransform = nodeParent.GetComponent<Transform>();

                parents[i] = nodeTransform;

                var parentIndex = node.parent?.index ?? -1;

                nodeTransform.SetParent(parentIndex >= 0 ? parents[parentIndex] : baseTransform);

                nodeTransform.LocalPosition = node.Position;
                nodeTransform.LocalRotation = node.Rotation;
                nodeTransform.LocalScale = node.Scale;

                foreach (var index in node.meshIndices)
                {
                    if (index < 0 || index >= asset.meshes.Count)
                    {
                        continue;
                    }

                    var mesh = asset.meshes[index];

                    var meshEntity = Entity.Create(mesh.name, typeof(Transform));

                    var meshTransform = meshEntity.GetComponent<Transform>();

                    var isSkinned = mesh.bones.Any(x => x.Length > 0);

                    meshTransform.SetParent(nodeTransform);

                    var outMesh = ResourceManager.instance.LoadMesh($"{asset.Guid}:{index}", true);
                    var outMaterials = mesh.submeshMaterialGuids.Select(x => ResourceManager.instance.LoadMaterial(x, true)).ToList();

                    if (outMesh != null)
                    {
                        if (isSkinned)
                        {
                            var skinnedRenderer = meshEntity.AddComponent<SkinnedMeshRenderer>();

                            skinnedRenderer.mesh = outMesh;
                            skinnedRenderer.materials = outMaterials;
                            skinnedRenderer.lighting = mesh.lighting;
                        }
                        else
                        {
                            var meshRenderer = meshEntity.AddComponent<MeshRenderer>();

                            meshRenderer.mesh = outMesh;
                            meshRenderer.materials = outMaterials;
                            meshRenderer.lighting = mesh.lighting;
                        }
                    }
                }
            }
        }

        return baseEntity;
    }

    public static void SaveAsset(IStapleAsset asset)
    {
        if(asset == null ||
            asset is not IGuidAsset guid ||
            guid.Guid == null)
        {
            return;
        }

        var entry = AssetDatabase.GetAssetEntry(guid.Guid);

        if(entry == null)
        {
            return;
        }

        StapleEditor.SaveAsset(entry.path, asset);
    }

    internal static string GetAssetCachePath(string basePath, string path, AppPlatform platform)
    {
        var cachePath = path.Replace("\\", "/");

        var index = Path.IsPathRooted(cachePath) ? cachePath.IndexOf("/Assets/") : -1;

        if (index >= 0)
        {
            cachePath = Path.Combine(basePath, "Cache", "Staging", platform.ToString(), cachePath.Substring(index + "/Assets/".Length)).Replace("\\", "/");
        }

        return cachePath;
    }

    internal static string GetLocalPath(string path)
    {
        var cachePath = path.Replace("\\", "/");

        var index = Path.IsPathRooted(cachePath) ? cachePath.IndexOf("/Assets/") : -1;

        if (index >= 0)
        {
            cachePath = cachePath.Substring(index + "/Assets/".Length);
        }

        return cachePath;
    }
}
