using MessagePack;
using NAudio.Wave;
using Newtonsoft.Json;
using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Baker;

static partial class Program
{
    private static void ProcessAudio(AppPlatform platform, string inputPath, string outputPath)
    {
        var audioFiles = new List<string>();

        foreach (var extension in AssetSerialization.AudioExtensions)
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
            var audioFileName = audioFiles[i];

            Console.WriteLine($"\t{audioFileName}");

            try
            {
                if (File.Exists(audioFileName) == false)
                {
                    Console.WriteLine($"\t\tError: {audioFileName} doesn't exist");

                    continue;
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"\t\tError: {audioFileName} doesn't exist");

                continue;
            }

            var guid = FindGuid<AudioClip>(audioFileName);

            var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(audioFileName));
            var file = Path.GetFileName(audioFileName).Replace(".meta", "");
            var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file);

            var index = outputFile.IndexOf(inputPath);

            if (index >= 0 && index < outputFile.Length)
            {
                outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
            }

            if (ShouldProcessFile(audioFileName, outputFile) == false &&
                ShouldProcessFile(audioFileName.Replace(".meta", ""), outputFile.Replace(".meta", "")) == false)
            {
                continue;
            }

            WorkScheduler.Dispatch(Path.GetFileName(audioFileName.Replace(".meta", "")), () =>
            {
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
                    var fileData = File.ReadAllBytes(audioFileName.Replace(".meta", ""));

                    var extension = Path.GetExtension(audioFileName.Replace(".meta", "").ToUpperInvariant());

                    var json = File.ReadAllText(audioFileName);

                    var metadata = JsonConvert.DeserializeObject<AudioClipMetadata>(json);

                    metadata.guid = guid;

                    var audioFormat = extension switch
                    {
                        ".MP3" => AudioClipFormat.MP3,
                        ".OGG" => AudioClipFormat.OGG,
                        ".WAV" => AudioClipFormat.WAV,
                        _ => AudioClipFormat.WAV,
                    };

                    if(audioFormat == AudioClipFormat.WAV)
                    {
                        switch (metadata.recompression)
                        {
                            case AudioRecompression.Vorbis:

                                try
                                {
                                    using var wavReader = new WaveFileReader(audioFileName.Replace(".meta", ""));

                                    var format = wavReader.WaveFormat;

                                    var sampleProvider = wavReader.ToSampleProvider();

                                    var buffer = new float[44100];

                                    var samples = new List<float>();

                                    var count = 0;

                                    while((count = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        samples.AddRange(buffer.Take(count));
                                    }

                                    fileData = AudioUtils.EncodeOGG(CollectionsMarshal.AsSpan(samples), format.Channels, format.SampleRate,
                                        metadata.recompressionQuality);

                                    audioFormat = AudioClipFormat.OGG;
                                }
                                catch (Exception)
                                {
                                }

                                break;
                        }
                    }

                    var audioClip = new SerializableAudioClip()
                    {
                        metadata = metadata,
                        fileData = fileData,
                        format = audioFormat,
                    };

                    var header = new SerializableAudioClipHeader();

                    using var stream = File.OpenWrite(outputFile);
                    using var writer = new BinaryWriter(stream);

                    var encoded = MessagePackSerializer.Serialize(header)
                        .Concat(MessagePackSerializer.Serialize(audioClip));

                    writer.Write(encoded.ToArray());
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to save audio: {e}");

                    try
                    {
                        File.Delete(outputFile);
                    }
                    catch (Exception)
                    {
                    }
                }
            });
        }
    }
}
