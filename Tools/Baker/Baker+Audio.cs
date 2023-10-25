using MessagePack;
using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Baker
{
    static partial class Program
    {
        private static void ProcessAudio(AppPlatform platform, string inputPath, string outputPath)
        {
            var audioFiles = new List<string>();

            foreach (var extension in audioExtensions)
            {
                try
                {
                    audioFiles.AddRange(Directory.GetFiles(inputPath, $"*.{extension}.meta", SearchOption.AllDirectories));
                }
                catch (Exception)
                {
                }
            }

            Console.WriteLine($"Processing {audioFiles.Count} audio files...");

            for (var i = 0; i < audioFiles.Count; i++)
            {
                //Guid collision fix
                Thread.Sleep(25);

                Console.WriteLine($"\t{audioFiles[i]}");

                try
                {
                    if (File.Exists(audioFiles[i]) == false)
                    {
                        Console.WriteLine($"\t\tError: {audioFiles[i]} doesn't exist");

                        continue;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: {audioFiles[i]} doesn't exist");

                    continue;
                }

                var guid = FindGuid<AudioClip>(audioFiles[i]);

                var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(audioFiles[i]));
                var file = Path.GetFileName(audioFiles[i]).Replace(".meta", "");
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
                    File.Delete($"{outputFile}.sbin");
                }
                catch (Exception)
                {
                }

                try
                {
                    File.Copy(audioFiles[i].Replace(".meta", ""), $"{outputFile}.sbin", true);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to save asset: {e}");
                }

                try
                {
                    var audioClip = new SerializableAudioClip()
                    {
                        metadata = new AudioClipMetadata()
                        {
                            guid = guid,
                        }
                    };

                    var header = new SerializableAudioClipHeader();

                    using (var stream = File.OpenWrite(outputFile))
                    {
                        using (var writer = new BinaryWriter(stream))
                        {
                            var encoded = MessagePackSerializer.Serialize(header)
                                .Concat(MessagePackSerializer.Serialize(audioClip));

                            writer.Write(encoded.ToArray());
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to save audio: {e}");

                    try
                    {
                        File.Delete(outputFile);
                        File.Delete($"{outputFile}.sbin");
                    }
                    catch (Exception)
                    {
                    }

                    continue;
                }
            }
        }
    }
}
