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
            }
            catch(Exception e)
            {
            }
        }

        try
        {
            var inputDirs = Directory.GetDirectories(inputPath).ToList();
            var outputDirs = Directory.GetDirectories(outputPath);

            var inputFiles = Directory.GetFiles(inputPath).ToList();
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
                if(inputFiles.All(x => Path.GetFileName(x) != Path.GetFileName(file)))
                {
                    DeleteFile(file);
                }
            }

            inputDirs = Directory.GetDirectories(inputPath).ToList();
            outputDirs = Directory.GetDirectories(outputPath);

            if(inputDirs.Count == outputDirs.Length)
            {
                for(var i = 0; i < inputDirs.Count; i++)
                {
                    CleanupUnusedFiles(platform, inputDirs[i], outputDirs[i]);
                }
            }
        }
        catch (Exception)
        {
        }
    }
}
