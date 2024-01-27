using Staple;
using System;
using System.Collections.Generic;
using System.IO;

namespace Baker
{
    static partial class Program
    {
        private static void ProcessAssets(AppPlatform platform, string inputPath, string outputPath)
        {
            var assetFiles = new List<string>();

            try
            {
                assetFiles.AddRange(Directory.GetFiles(inputPath, $"*.asset", SearchOption.AllDirectories));
            }
            catch (Exception)
            {
            }

            Console.WriteLine($"Processing {assetFiles.Count} assets...");

            for (var i = 0; i < assetFiles.Count; i++)
            {
                Console.WriteLine($"\t{assetFiles[i]}");

                try
                {
                    if (File.Exists(assetFiles[i]) == false)
                    {
                        Console.WriteLine($"\t\tError: {assetFiles[i]} doesn't exist");

                        continue;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: {assetFiles[i]} doesn't exist");

                    continue;
                }

                var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(assetFiles[i]));
                var file = Path.GetFileName(assetFiles[i]);
                var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file);

                var index = outputFile.IndexOf(inputPath);

                if (index >= 0 && index < outputFile.Length)
                {
                    outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
                }

                Console.WriteLine($"\t\t -> {outputFile}");

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
                }
                catch (Exception)
                {
                }

                try
                {
                    Directory.CreateDirectory(outputPath);
                }
                catch (Exception)
                {
                }

                try
                {
                    File.Delete(outputFile);
                }
                catch (Exception)
                {
                }

                try
                {
                    File.Copy(assetFiles[i], outputFile, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to save asset: {e}");
                }
            }
        }
    }
}
