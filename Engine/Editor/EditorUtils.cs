using System.Text;
using System;
using System.IO;

namespace Staple.Editor;

public static class EditorUtils
{
    private static readonly string[] byteSizes = new string[]
    {
        "B",
        "KB",
        "MB",
        "GB",
        "TB"
    };

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

        editor.showingProgress = true;
        editor.progressFraction = 0;

        editor.StartBackgroundTask((ref float progressFraction) =>
        {
            editor.RefreshAssets(updateProject);

            ThumbnailCache.Clear();

            onFinish?.Invoke();

            return true;
        });
    }
}
