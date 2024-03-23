using Staple;
using System;
using System.IO;
using System.Linq;

namespace Baker;

static partial class Program
{
    public static void CleanupUnusedFiles(AppPlatform platform, string inputPath, string outputPath)
    {
        void DeleteDirectory(string path)
        {
            try
            {
                Directory.Delete(path, true);

                Console.WriteLine($"\t Deleted {path}");
            }
            catch (Exception e)
            {
            }
        }

        void DeleteFile(string path)
        {
            try
            {
                File.Delete(path);

                Console.WriteLine($"\t Deleted {path}");
            }
            catch (Exception e)
            {
            }
        }

        try
        {
            var inputDirs = Directory.GetDirectories(inputPath);
            var outputDirs = Directory.GetDirectories(outputPath);

            var inputFiles = Directory.GetFiles(inputPath);
            var outputFiles = Directory.GetFiles(outputPath);

            foreach(var dir in outputDirs)
            {
                if(inputDirs.All(x => Path.GetFileName(x) != Path.GetFileName(dir)) &&
                    Enum.GetValues<Renderer>().Any(x => dir.Contains(x.ToString())) == false)
                {
                    DeleteDirectory(dir);
                }
            }

            foreach(var file in outputFiles)
            {
                var fileName = Path.GetFileName(file);

                if(fileName == "AppSettings" || fileName == "SceneList")
                {
                    continue;
                }

                if(inputFiles.All(x => Path.GetFileName(x) != Path.GetFileName(file)))
                {
                    DeleteFile(file);
                }
            }

            inputDirs = Directory.GetDirectories(inputPath);
            outputDirs = Directory.GetDirectories(outputPath);

            for(var i = 0; i < inputDirs.Length; i++)
            {
                var input = inputDirs[i];
                var similar = outputDirs.FirstOrDefault(x => Path.GetFileName(x) == Path.GetFileName(inputDirs[i]));

                if(similar != null)
                {
                    CleanupUnusedFiles(platform, input, similar);
                }
            }
        }
        catch (Exception)
        {
        }
    }
}
