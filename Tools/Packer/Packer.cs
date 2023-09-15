using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Packer
{
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
        private static List<string> inputDirectories = new();

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage:\n" +
                    "Packer\n" +
                    "\t-o [path]: set output file name\n" +
                    "\t-i [path]: add input directory (can repeat)\n" +
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
                            Console.WriteLine("Invalid argument `-o`: missing output path");

                            Environment.Exit(1);

                            return;
                        }

                        outputPath = args[i + 1];

                        i++;

                        break;

                    case "-i":

                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("Invalid argument `-i`: missing input directory path");

                            Environment.Exit(1);

                            return;
                        }

                        inputDirectories.Add(args[i + 1]);

                        i++;

                        break;

                    case "-p":

                        mode = Mode.Pack;

                        break;

                    case "-up":

                        mode = Mode.Unpack;

                        break;

                    case "-l":

                        mode = Mode.List;

                        break;
                }
            }

            switch (mode)
            {
                case Mode.List:

                    {
                        var input = inputDirectories.FirstOrDefault();

                        if (input == null)
                        {
                            Console.WriteLine($"Failed to list files: no input was set. Make sure to use `-i` to specify the file.");

                            Environment.Exit(1);
                        }

                        try
                        {
                            using var stream = new FileStream(input, FileMode.Open);

                            var pack = new ResourcePak();

                            if(pack.Deserialize(stream) == false)
                            {
                                Console.WriteLine($"Failed to load package at {input}");

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
                            Console.WriteLine($"Failed to list files at {input}");

                            Environment.Exit(1);
                        }
                    }

                    break;

                case Mode.Pack:

                    {
                        if((outputPath?.Length ?? 0) == 0)
                        {
                            Console.WriteLine($"Unable to pack: Missing `-o` parameter.");

                            Environment.Exit(1);
                        }

                        var filePaths = new List<string>();
                        var localFilePaths = new List<string>();
                        var fileGuids = new List<string>();
                        var fileStreams = new List<Stream>();

                        void Recursive(string basePath, string current)
                        {
                            var directories = Directory.GetDirectories(current);
                            var files = Directory.GetFiles(current);

                            foreach(var directory in directories)
                            {
                                Recursive(basePath, directory);
                            }

                            foreach(var file in files)
                            {
                                if(file.EndsWith(".meta"))
                                {
                                    continue;
                                }

                                var localPath = Path.GetRelativePath(basePath, file).Replace(Path.DirectorySeparatorChar, '/');
                                var guid = PackerUtils.ExtractGuid(file) ?? Guid.NewGuid().ToString();

                                filePaths.Add(file);
                                localFilePaths.Add(localPath);
                                fileStreams.Add(new FileStream(file, FileMode.Open));
                                fileGuids.Add(guid);
                            }
                        }

                        try
                        {
                            foreach(var input in inputDirectories)
                            {
                                var normalized = Path.GetFullPath(input);

                                Recursive(normalized, normalized);
                            }
                        }
                        catch(Exception)
                        {
                            Console.WriteLine($"Failed to scan for files and folders, aborting...");

                            foreach(var stream in fileStreams)
                            {
                                stream.Dispose();
                            }

                            Environment.Exit(1);
                        }

                        var resourcePak = new ResourcePak();

                        for(var i = 0; i < filePaths.Count; i++)
                        {
                            resourcePak.AddEntry(fileGuids[i], localFilePaths[i], fileStreams[i]);
                        }

                        try
                        {
                            using var stream = new FileStream(outputPath, FileMode.Create);

                            if(resourcePak.Serialize(stream) == false)
                            {
                                Console.WriteLine($"Failed to save pak, aborting...");

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
                            Console.WriteLine($"Failed to save pak, aborting... (Exception: {e})");

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
                            Console.WriteLine($"Unable to unpack: Missing `-i` parameter.");

                            Environment.Exit(1);
                        }

                        if ((outputPath?.Length ?? 0) == 0)
                        {
                            Console.WriteLine($"Unable to unpack: Missing `-o` parameter.");

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
                                Console.WriteLine($"Failed to unpack pak, aborting... (File failed to be read)");

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

                                using var fileStream = resourcePak.OpenGuid(file.guid);
                                using var outStream = File.OpenWrite(Path.Combine(outputPath, file.path));

                                fileStream.CopyTo(outStream);
                            }

                            Console.WriteLine($"Extracted {resourcePak.Files.Count()} files");
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine($"Failed to unpack pak, aborting... (Exception: {e})");

                            Environment.Exit(1);
                        }
                    }

                    break;
            }
        }
    }
}