using Newtonsoft.Json;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Packer;

static class Program
{
    enum Mode
    {
        Pack,
        Unpack,
        List
    }

    private static Mode mode;
    private static string outputPath;
    private static bool recursive = false;
    private static List<string> inputDirectories = new();

    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage:\n" +
                "Packer\n" +
                "\t-o [path]: set output file name\n" +
                "\t-i [path]: add input directory (can repeat)\n" +
                "\t-r: search subfolders as well\n" +
                "\t-p: set mode to pack\n" +
                "\t-up: set mode to unpack\n" +
                "\t-l: set mode to list\n");

            Environment.Exit(1);

            return;
        }

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-o":

                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("Error: Invalid argument `-o`: missing output path");

                        Environment.Exit(1);

                        return;
                    }

                    outputPath = args[i + 1];

                    i++;

                    break;

                case "-i":

                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("Error: Invalid argument `-i`: missing input directory path");

                        Environment.Exit(1);

                        return;
                    }

                    inputDirectories.Add(args[i + 1]);

                    i++;

                    break;

                case "-p":

                    mode = Mode.Pack;

                    break;

                case "-r":

                    recursive = true;

                    break;

                case "-up":

                    mode = Mode.Unpack;

                    break;

                case "-l":

                    mode = Mode.List;

                    break;
            }
        }

        Console.WriteLine($"Packer starting with parameters:\n" +
            $"Mode: {mode}\n" +
            $"Input Dirs:\n{string.Join("\n", inputDirectories)}\n" +
            $"Output: {outputPath}\n");

        switch (mode)
        {
            case Mode.List:

                {
                    var input = inputDirectories.FirstOrDefault();

                    if (input == null)
                    {
                        Console.WriteLine($"Error: Failed to list files: no input was set. Make sure to use `-i` to specify the file.");

                        Environment.Exit(1);
                    }

                    try
                    {
                        using var stream = new FileStream(input, FileMode.Open);

                        var pack = new ResourcePak();

                        if(pack.Deserialize(stream) == false)
                        {
                            Console.WriteLine($"Error: Failed to load package at {input}");

                            Environment.Exit(1);
                        }

                        foreach(var entry in pack.Files)
                        {
                            Console.WriteLine($"\t{entry.path} ({entry.guid}, {entry.size})");
                        }

                        Console.WriteLine($"{pack.Files.Count()} files");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Error: Failed to list files at {input}");

                        Environment.Exit(1);
                    }
                }

                break;

            case Mode.Pack:

                {
                    if((outputPath?.Length ?? 0) == 0)
                    {
                        Console.WriteLine($"Error: Unable to pack: Missing `-o` parameter.");

                        Environment.Exit(1);
                    }

                    var targetPakName = Path.GetFileNameWithoutExtension(outputPath);

                    var filePaths = new List<string>();
                    var localFilePaths = new List<string>();
                    var fileGuids = new List<string>();
                    var fileTypes = new List<string>();
                    var fileStreams = new List<Stream>();

                    /*
                     * We want to filter for invalid subfolders but this can be tricky,
                     * because this is recursive this means that we may go into a folder that has no pakName
                     * and therefore think it's invalid.
                     * We can't assume pakName-less folders are valid either.
                     * So we basically start with an empty pak name and keep searching till we find a valid pak name and then add those files.
                     * However, we still gotta recursive because our folders might be someplace else, so we have to keep going either way.
                    */
                    (bool, string) IsValidSubfolder(string path, string currentPakName)
                    {
                        try
                        {
                            if(File.Exists($"{path}.meta"))
                            {
                                var json = File.ReadAllText($"{path}.meta");

                                var folderAsset = JsonConvert.DeserializeObject<FolderAsset>(json, Staple.Tooling.Utilities.JsonSettings);

                                var pakName = folderAsset.pakName;

                                Console.WriteLine($"ValidSubfolder: {pakName ?? "null"} {currentPakName ?? "null"} {targetPakName}");

                                if(pakName == targetPakName)
                                {
                                    Console.WriteLine("Valid PakName");
                                    return (true, targetPakName);
                                }
                                else if(folderAsset.pakName == null)
                                {
                                    Console.WriteLine("Valid PakName null");
                                    return (currentPakName == targetPakName, null);
                                }

                                return (false, folderAsset.pakName);
                            }

                            return (currentPakName == targetPakName, null);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Warning: Subfolder {path} ignored due to error: {e}");

                            return (false, null);
                        }
                    }

                    void Recursive(string basePath, string current, string currentPakName)
                    {
                        var directories = Directory.GetDirectories(current);
                        var files = Directory.GetFiles(current);

                        void Finish()
                        {
                            if (recursive)
                            {
                                foreach (var directory in directories)
                                {
                                    Recursive(basePath, directory, currentPakName);
                                }
                            }
                        }

                        var validation = IsValidSubfolder(current, currentPakName);

                        if (validation.Item1 == false)
                        {
                            currentPakName = validation.Item2 ?? currentPakName;

                            Finish();

                            return;
                        }

                        if(validation.Item2 != null && validation.Item2 != currentPakName)
                        {
                            currentPakName = validation.Item2;
                        }

                        foreach (var file in files)
                        {
                            if(file.EndsWith(".meta"))
                            {
                                continue;
                            }

                            var localPath = Path.GetRelativePath(basePath, file).Replace(Path.DirectorySeparatorChar, '/');
                            var guid = PackerUtils.ExtractGuid(file, out var typeName) ?? Guid.NewGuid().ToString();

                            if(fileGuids.Any(x => x == guid))
                            {
                                Console.WriteLine($"Warning: Duplicate guid found for file {file}: {guid}");
                            }

                            filePaths.Add(file);
                            localFilePaths.Add(localPath);
                            fileTypes.Add(typeName);
                            fileStreams.Add(new FileStream(file, FileMode.Open));
                            fileGuids.Add(guid);
                        }

                        Finish();
                    }

                    try
                    {
                        foreach(var input in inputDirectories)
                        {
                            var normalized = Path.GetFullPath(input);

                            Recursive(normalized, normalized, targetPakName == "Resources" || targetPakName.StartsWith("DefaultResources-") ? targetPakName : null);
                        }
                    }
                    catch(Exception)
                    {
                        Console.WriteLine($"Error: Failed to scan for files and folders, aborting...");

                        foreach(var stream in fileStreams)
                        {
                            stream.Dispose();
                        }

                        Environment.Exit(1);
                    }

                    if(filePaths.Count == 0)
                    {
                        Console.WriteLine($"Warning: No valid files were found to pack, exiting...");

                        Environment.Exit(0);
                    }

                    var resourcePak = new ResourcePak();

                    for(var i = 0; i < filePaths.Count; i++)
                    {
                        resourcePak.AddEntry(fileGuids[i], localFilePaths[i], fileTypes[i], fileStreams[i]);
                    }

                    try
                    {
                        using var stream = new FileStream(outputPath, FileMode.Create);

                        if(resourcePak.Serialize(stream) == false)
                        {
                            Console.WriteLine($"Error: Failed to save pak, aborting...");

                            stream.Dispose();

                            foreach(var s in fileStreams)
                            {
                                s.Dispose();
                            }

                            File.Delete(outputPath);

                            Environment.Exit(1);
                        }
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine($"Error: Failed to save pak, aborting... (Exception: {e})");

                        foreach (var s in fileStreams)
                        {
                            s.Dispose();
                        }

                        File.Delete(outputPath);

                        Environment.Exit(1);
                    }

                    foreach (var s in fileStreams)
                    {
                        s.Dispose();
                    }

                    Console.WriteLine($"Saved Pak at {outputPath} ({filePaths.Count} entries)");
                }

                break;

            case Mode.Unpack:

                {
                    var inputPath = inputDirectories.FirstOrDefault();

                    if ((inputPath?.Length ?? 0) == 0)
                    {
                        Console.WriteLine($"Error: Unable to unpack: Missing `-i` parameter.");

                        Environment.Exit(1);
                    }

                    if ((outputPath?.Length ?? 0) == 0)
                    {
                        Console.WriteLine($"Error: Unable to unpack: Missing `-o` parameter.");

                        Environment.Exit(1);
                    }

                    try
                    {
                        Directory.CreateDirectory(outputPath);
                    }
                    catch(Exception)
                    {
                    }

                    try
                    {
                        using var stream = File.OpenRead(inputPath);

                        var resourcePak = new ResourcePak();

                        if(resourcePak.Deserialize(stream) == false)
                        {
                            Console.WriteLine($"Error: Failed to unpack pak, aborting... (File failed to be read)");

                            Environment.Exit(1);
                        }

                        foreach(var file in resourcePak.Files)
                        {
                            try
                            {
                                Directory.CreateDirectory(Path.Combine(outputPath, Path.GetDirectoryName(file.path)));
                            }
                            catch(Exception)
                            {
                            }

                            if(resourcePak.Files.Count(x => x.guid == file.guid) > 1)
                            {
                                Console.WriteLine($"Warning: Duplicate guid {file.guid} found for file {file.path}");

                                using var fileStream = resourcePak.Open(file.path);
                                using var outStream = File.OpenWrite(Path.Combine(outputPath, file.path));

                                fileStream.CopyTo(outStream);
                            }
                            else
                            {
                                using var fileStream = resourcePak.OpenGuid(file.guid);
                                using var outStream = File.OpenWrite(Path.Combine(outputPath, file.path));

                                fileStream.CopyTo(outStream);
                            }
                        }

                        Console.WriteLine($"Extracted {resourcePak.Files.Count()} files");
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine($"Error: Failed to unpack pak, aborting... (Exception: {e})");

                        Environment.Exit(1);
                    }
                }

                break;
        }
    }
}