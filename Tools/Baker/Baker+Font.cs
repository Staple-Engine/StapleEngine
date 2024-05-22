using MessagePack;
using Newtonsoft.Json;
using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Baker;

static partial class Program
{
    private static void ProcessFonts(AppPlatform platform, string inputPath, string outputPath)
    {
        var fontFiles = new List<string>();

        foreach (var extension in AssetSerialization.FontExtensions)
        {
            try
            {
                fontFiles.AddRange(Directory.GetFiles(inputPath, $"*.{extension}.meta", SearchOption.AllDirectories));
            }
            catch (Exception)
            {
            }
        }

        Console.WriteLine($"Processing {fontFiles.Count} font files...");

        for (var i = 0; i < fontFiles.Count; i++)
        {
            var fontFileName = fontFiles[i];

            Console.WriteLine($"\t{fontFileName}");

            try
            {
                if (File.Exists(fontFileName) == false)
                {
                    Console.WriteLine($"\t\tError: {fontFileName} doesn't exist");

                    continue;
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"\t\tError: {fontFileName} doesn't exist");

                continue;
            }

            var guid = FindGuid<FontAsset>(fontFileName);

            var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(fontFileName));
            var file = Path.GetFileName(fontFileName).Replace(".meta", "");
            var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file);

            var index = outputFile.IndexOf(inputPath);

            if (index >= 0 && index < outputFile.Length)
            {
                outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
            }

            WorkScheduler.Dispatch(Path.GetFileName(fontFileName.Replace(".meta", "")), () =>
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
                    var fileData = File.ReadAllBytes(fontFileName.Replace(".meta", ""));

                    var extension = Path.GetExtension(fontFileName.Replace(".meta", "").ToUpperInvariant());

                    var json = File.ReadAllText(fontFileName);

                    var metadata = JsonConvert.DeserializeObject<FontMetadata>(json);

                    metadata.guid = guid;

                    var font = new SerializableFont()
                    {
                        metadata = metadata,
                        fontData = fileData,
                    };

                    var textFont = new TextFont()
                    {
                        fontData = fileData,
                        includedRanges = metadata.includedCharacterSets,
                    };

                    foreach(var size in metadata.bakedSizes)
                    {
                        if(font.sizes.Any(x => x.fontSize == size) ||
                            textFont.MakePixelData(size, metadata.textureSize, out var textureData,
                                out var ascent, out var descent, out var lineGap, out var glyphs) == false)
                        {
                            continue;
                        }

                        var piece = new FontSizePiece()
                        {
                            ascent = ascent,
                            descent = descent,
                            fontSize = size,
                            lineGap = lineGap,
                            textureData = textureData,
                        };

                        foreach(var pair in glyphs)
                        {
                            piece.glyphs.Add(new()
                            {
                                codepoint = pair.Key,
                                xAdvance = pair.Value.xAdvance,
                                xOffset = pair.Value.xOffset,
                                yOffset = pair.Value.yOffset,
                                bounds = new Vector4Holder(new Vector4(pair.Value.bounds.left, pair.Value.bounds.right, pair.Value.bounds.top, pair.Value.bounds.bottom))
                            });
                        }

                        font.sizes.Add(piece);
                    }

                    var header = new SerializableFontHeader();

                    using var stream = File.OpenWrite(outputFile);
                    using var writer = new BinaryWriter(stream);

                    var encoded = MessagePackSerializer.Serialize(header)
                        .Concat(MessagePackSerializer.Serialize(font));

                    writer.Write(encoded.ToArray());
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to save font: {e}");

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
