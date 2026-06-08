using Staple;
using System;
using System.IO;

namespace Baker;

static partial class Program
{
    private static void ProcessFolders(AppPlatform platform, string inputPath, string outputPath)
    {
        try
        {
            void Recursive(string basePath)
            {
                var directories = Directory.GetDirectories(basePath);

                foreach (var directory in directories)
                {
                    var previous = Path.Combine(directory, "..");

                    var p = Path.Combine(previous, $"{Path.GetFileName(directory)}.meta");

                    if (File.Exists(p))
                    {
                        var d = Path.GetRelativePath(inputPath, previous);
                        var file = $"{Path.GetFileName(directory)}.meta";
                        var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, d, file);

                        var index = outputFile.IndexOf(inputPath);

                        if (index >= 0 && index < outputFile.Length)
                        {
                            outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
                        }

                        File.Copy($"{directory}.meta", outputFile, true);
                    }

                    Recursive(directory);
                }
            }

            Recursive(inputPath);
        }
        catch(Exception)
        {
        }
    }
}
