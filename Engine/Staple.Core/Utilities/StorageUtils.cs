using System;
using System.IO;

namespace Staple.Internal;

internal class StorageUtils
{
    /// <summary>
    /// Attempts to find the staple editor's path
    /// </summary>
    public static Lazy<string> EditorPath = new(() =>
    {
        var basePath = Path.Combine(Storage.StapleBasePath, "Staging");

        try
        {
            if (!Directory.Exists(basePath))
            {
                basePath = Path.Combine(Storage.StapleBasePath, "Editor");
            }

            if (!Directory.Exists(basePath))
            {
                return null;
            }
        }
        catch (Exception)
        {
        }

        return basePath;
    });

    /// <summary>
    /// Gets the root path to an asset in a project
    /// </summary>
    /// <param name="basePath">The base path of the project</param>
    /// <param name="assetPath">The path of the asset (not a GUID)</param>
    /// <returns>The root path, or null</returns>
    internal static string GetRootPath(string basePath, string assetPath)
    {
        if (assetPath == null)
        {
            return null;
        }

        if (assetPath.StartsWith("Assets/"))
        {
            return Path.Combine(basePath, assetPath);
        }

        return Path.Combine(basePath, "Cache", "Packages", assetPath);
    }

    /// <summary>
    /// Attempts to copy a file
    /// </summary>
    /// <param name="source">The source file</param>
    /// <param name="destination">The destination file</param>
    /// <returns>Whether it did so successfully</returns>
    public static bool CopyFile(string source, string destination)
    {
        CreateDirectory(Path.GetDirectoryName(destination));

        try
        {
            File.Copy(source, destination, true);
        }
        catch (Exception)
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
        catch (Exception)
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
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Attempts to delete a directory
    /// </summary>
    /// <param name="path">The directory's path</param>
    public static void DeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, true);
        }
        catch (Exception)
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
        if (Path.GetFileName(source).StartsWith('.'))
        {
            //Silently ignore hidden folders
            return true;
        }

        try
        {
            if (!Directory.Exists(source))
            {
                //Silently succeed
                return true;
            }
        }
        catch (Exception)
        {
        }

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
                if (!CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory))))
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
}
