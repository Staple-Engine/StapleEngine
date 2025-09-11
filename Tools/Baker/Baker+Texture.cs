using MessagePack;
using Newtonsoft.Json;
using Staple;
using Staple.Internal;
using Staple.Tooling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Baker;

static partial class Program
{
    private class DuplicateSpriteInfo
    {
        public TextureSpriteRotation rotation;
        public Rect original;
    }

    private class RawTextureInfo
    {
        public RawTextureData textureData;
        public Rect location;

        public List<DuplicateSpriteInfo> duplicates = [];
    }

    private static void ProcessTextures(AppPlatform platform, string texturecPath, string inputPath, string outputPath)
    {
        var textureFiles = new List<string>();

        foreach (var extension in AssetSerialization.TextureExtensions)
        {
            try
            {
                textureFiles.AddRange(Directory.GetFiles(inputPath, $"*.{extension}.meta", SearchOption.AllDirectories));
            }
            catch (Exception)
            {
            }
        }

        Console.WriteLine($"Processing {textureFiles.Count} textures...");

        for (var i = 0; i < textureFiles.Count; i++)
        {
            var textureFileName = textureFiles[i];

            //Console.WriteLine($"\t{textureFileName.Replace(".meta", "")}");

            try
            {
                if (File.Exists(textureFileName.Replace(".meta", "")) == false)
                {
                    Console.WriteLine($"\t\tError: {textureFileName.Replace(".meta", "")} doesn't exist");

                    continue;
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"\t\tError: {textureFileName.Replace(".meta", "")} doesn't exist");

                continue;
            }

            var guid = FindGuid<Texture>(textureFileName);

            var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(textureFileName));
            var file = Path.GetFileName(textureFileName);
            var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file.Replace(".meta", ""));

            var index = outputFile.IndexOf(inputPath);

            if (index >= 0 && index < outputFile.Length)
            {
                outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
            }

            processedTextures.AddOrSetKey(textureFileName.Replace("\\", "/"), guid);

            if (ShouldProcessFile(textureFileName, outputFile) == false &&
                ShouldProcessFile(textureFileName.Replace(".meta", ""), outputFile.Replace(".meta", "")) == false)
            {
                continue;
            }

            WorkScheduler.Main.Dispatch(Path.GetFileName(textureFileName.Replace(".meta", "")), () =>
            {
                var inputFile = textureFileName.Replace(".meta", "");

                //Console.WriteLine($"\t\t -> {outputFile}");

                string text;
                TextureMetadata metadata;

                try
                {
                    text = File.ReadAllText(textureFileName);
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Failed to read file");

                    return;
                }

                try
                {
                    metadata = JsonConvert.DeserializeObject<TextureMetadata>(text);
                    metadata.guid = guid;
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Metadata is corrupted");

                    return;
                }

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

                var format = metadata.format;
                var quality = metadata.quality;
                var maxSize = metadata.maxSize;
                var premultiplyAlpha = metadata.premultiplyAlpha;

                if (metadata.overrides.TryGetValue(platform, out var overrides) && overrides.shouldOverride)
                {
                    format = overrides.format;
                    quality = overrides.quality;
                    maxSize = overrides.maxSize;
                    premultiplyAlpha = overrides.premultiplyAlpha;
                }

                var extension = Path.GetExtension(inputFile).Substring(1);

                var replacedInput = false;

                if (AssetSerialization.ResizableTextureExtensions.Contains(extension))
                {
                    RawTextureData textureData;

                    try
                    {
                        textureData = Texture.LoadStandard(File.ReadAllBytes(inputFile), StandardTextureColorComponents.RGBA);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"\t\tFailed to load image data");

                        return;
                    }

                    if (textureData == null)
                    {
                        Console.WriteLine($"\t\tFailed to load image data");

                        return;
                    }

                    var scale = 1.0f;

                    if (textureData.width > maxSize || textureData.height > maxSize)
                    {
                        if (textureData.width > textureData.height)
                        {
                            scale = maxSize / (float)textureData.width;
                        }
                        else
                        {
                            scale = maxSize / (float)textureData.height;
                        }

                        textureData.Resize((int)(textureData.width * scale), (int)(textureData.height * scale));
                    }

                    inputFile = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
                    replacedInput = true;

                    var png = textureData.EncodePNG();

                    try
                    {
                        File.WriteAllBytes(inputFile, png);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("\t\tFailed to process: I/O Error");

                        return;
                    }

                    if (metadata.type == TextureType.Sprite)
                    {
                        var spriteTextures = new List<RawTextureInfo>();

                        switch (metadata.spriteTextureMethod)
                        {
                            case SpriteTextureMethod.Single:

                                metadata.sprites.Clear();

                                if (textureData != null)
                                {
                                    metadata.sprites.Add(new TextureSpriteInfo()
                                    {
                                        rect = new Rect(Vector2Int.Zero, new Vector2Int(textureData.width, textureData.height))
                                    });
                                }

                                break;

                            case SpriteTextureMethod.Grid:

                                metadata.sprites.Clear();

                                var gridSize = metadata.spriteTextureGridSize;

                                if (scale != 1)
                                {
                                    gridSize.X = Staple.Math.RoundToInt(gridSize.X * scale);
                                    gridSize.Y = Staple.Math.RoundToInt(gridSize.Y * scale);
                                }

                                if (gridSize.X > 0 &&
                                    gridSize.Y > 0)
                                {
                                    bool ValidRegion(int x, int y)
                                    {
                                        var rawData = new RawTextureData()
                                        {
                                            colorComponents = textureData.colorComponents,
                                            width = gridSize.X,
                                            height = gridSize.Y,
                                            data = new byte[gridSize.X * gridSize.Y * 4],
                                        };

                                        var pitch = rawData.width * 4;

                                        for (int regionY = 0, yPos = (y * textureData.width) * 4, destYPos = 0; regionY < gridSize.Y;
                                            regionY++, yPos += textureData.width * 4, destYPos += rawData.width * 4)
                                        {
                                            Buffer.BlockCopy(textureData.data, yPos + x * 4, rawData.data, destYPos, pitch);
                                        }

                                        for (int regionY = 0, yPos = 0; regionY < rawData.height; regionY++, yPos += pitch)
                                        {
                                            for (int regionX = 0, xPos = 0; regionX < rawData.width; regionX++, xPos += 4)
                                            {
                                                if (rawData.data[xPos + yPos + 3] != 0)
                                                {
                                                    if (metadata.shouldPack)
                                                    {
                                                        spriteTextures.Add(new RawTextureInfo()
                                                        {
                                                            textureData = rawData,
                                                            location = new Rect(new Vector2Int(x, y), gridSize)
                                                        });
                                                    }

                                                    return true;
                                                }
                                            }
                                        }

                                        return false;
                                    }

                                    var size = new Vector2Int(textureData.width / gridSize.X,
                                        textureData.height / gridSize.Y);

                                    for (int y = 0, yPos = 0; y < size.Y; y++, yPos += gridSize.Y)
                                    {
                                        for (int x = 0, xPos = 0; x < size.X; x++, xPos += gridSize.X)
                                        {
                                            if (ValidRegion(xPos, yPos) == false)
                                            {
                                                continue;
                                            }

                                            metadata.sprites.Add(new TextureSpriteInfo()
                                            {
                                                rect = new Rect(new Vector2Int(xPos, yPos), gridSize)
                                            });
                                        }
                                    }
                                }

                                break;
                        }

                        if (metadata.shouldPack)
                        {
                            if (spriteTextures.Count > 1)
                            {
                                metadata.sprites.Clear();

                                if (metadata.trimDuplicates)
                                {
                                    //Console.WriteLine($"\t\tRemoving duplicates in {spriteTextures.Count} sprites");

                                    for (var j = 0; j < spriteTextures.Count; j++)
                                    {
                                        var first = spriteTextures[j];

                                        /*
                                        Console.WriteLine($"Validating {j + 1}/{spriteTextures.Count} " +
                                            $"({first.location.left}, {first.location.top}, {first.location.Width}, {first.location.Height})");
                                        */

                                        var stride = first.textureData.width * 4;

                                        for (var k = spriteTextures.Count - 1; k > j; k--)
                                        {
                                            var second = spriteTextures[k];

                                            if (first.textureData.width != second.textureData.width ||
                                                first.textureData.height != second.textureData.height)
                                            {
                                                continue;
                                            }

                                            //Remove duplicates
                                            if (first.textureData.data.SequenceEqual(second.textureData.data))
                                            {
                                                /*
                                                Console.WriteLine($"\t\tRemoved 1:1 duplicate at index {k} " +
                                                    $"({second.location.left}, {second.location.top}, {second.location.Width}, {second.location.Height})");
                                                */

                                                first.duplicates.Add(new DuplicateSpriteInfo()
                                                {
                                                    rotation = TextureSpriteRotation.Duplicate,
                                                    original = second.location,
                                                });

                                                spriteTextures.RemoveAt(k);

                                                continue;
                                            }

                                            var valid = true;

                                            //Test flip Y
                                            for (int y = 0, yPos = 0, destYPos = ((first.textureData.height - 1) * first.textureData.width) * 4;
                                                y < first.textureData.height;
                                                y++, yPos += stride, destYPos -= stride)
                                            {
                                                for (var l = 0; l < stride; l++)
                                                {
                                                    if (first.textureData.data[yPos + l] != second.textureData.data[destYPos + l])
                                                    {
                                                        valid = false;

                                                        break;
                                                    }
                                                }

                                                if (valid == false)
                                                {
                                                    break;
                                                }
                                            }

                                            if (valid)
                                            {
                                                /*
                                                Console.WriteLine($"\t\tRemoved Flipped Y duplicate at index {k} " +
                                                    $"({second.location.left}, {second.location.top}, {second.location.Width}, {second.location.Height})");
                                                */

                                                first.duplicates.Add(new DuplicateSpriteInfo()
                                                {
                                                    rotation = TextureSpriteRotation.FlipY,
                                                    original = second.location,
                                                });

                                                spriteTextures.RemoveAt(k);

                                                continue;
                                            }

                                            valid = true;

                                            //Test flip X
                                            for (int y = 0, yPos = 0; y < first.textureData.height; y++, yPos += stride)
                                            {
                                                for (int x = 0, xPos = 0, destXPos = stride - 4; x < first.textureData.width; x++, xPos += 4, destXPos -= 4)
                                                {
                                                    for (var l = 0; l < 4; l++)
                                                    {
                                                        if (first.textureData.data[yPos + xPos + l] != second.textureData.data[yPos + destXPos + l])
                                                        {
                                                            valid = false;

                                                            break;
                                                        }
                                                    }

                                                    if (valid == false)
                                                    {
                                                        break;
                                                    }
                                                }

                                                if (valid == false)
                                                {
                                                    break;
                                                }
                                            }

                                            if (valid)
                                            {
                                                /*
                                                Console.WriteLine($"\t\tRemoved Flipped X duplicate at index {k} " +
                                                    $"({second.location.left}, {second.location.top}, {second.location.Width}, {second.location.Height})");
                                                */

                                                first.duplicates.Add(new DuplicateSpriteInfo()
                                                {
                                                    rotation = TextureSpriteRotation.FlipX,
                                                    original = second.location,
                                                });

                                                spriteTextures.RemoveAt(k);

                                                continue;
                                            }
                                        }
                                    }
                                }

                                var packed = Texture.PackTextures(spriteTextures.Select(x => x.textureData).ToArray(), 32, 32, maxSize,
                                    metadata.padding, out var rects, out textureData);

                                if (packed)
                                {
                                    for (var j = 0; j < spriteTextures.Count; j++)
                                    {
                                        var sprite = spriteTextures[j];
                                        var rect = rects[j];

                                        metadata.sprites.Add(new TextureSpriteInfo()
                                        {
                                            rect = rect,
                                            originalRect = sprite.location,
                                            rotation = TextureSpriteRotation.None,
                                        });

                                        foreach (var duplicate in sprite.duplicates)
                                        {
                                            metadata.sprites.Add(new TextureSpriteInfo()
                                            {
                                                rect = rect,
                                                originalRect = duplicate.original,
                                                rotation = duplicate.rotation,
                                            });
                                        }
                                    }

                                    var outData = textureData.EncodePNG();

                                    if (outData != null)
                                    {
                                        File.WriteAllBytes(inputFile, outData);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    metadata.sprites.Clear();
                }

                var parameters = $"-t {format} -q {quality.ToString().ToLowerInvariant()} --as dds";

                if (premultiplyAlpha)
                {
                    parameters += " --pma";
                }

                if (metadata.isLinear)
                {
                    parameters += " --linear";
                }

                if (metadata.useMipmaps)
                {
                    parameters += " --mips";
                }

                switch (metadata.type)
                {
                    case TextureType.NormalMap:

                        parameters += " --normalmap";

                        break;
                }

                try
                {
                    File.Delete(outputFile);
                }
                catch (Exception)
                {
                }

                var outputFileTemp = $"{outputFile}_temp";

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = texturecPath,
                        Arguments = $"-f \"{inputFile}\" -o \"{outputFileTemp}\" {parameters}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                    }
                };

                var log = new StringBuilder();

                Utilities.ExecuteAndCollectProcess(process, (msg) => log.AppendLine(msg));

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"\t\t\tArguments: {process.StartInfo.Arguments}");

                    Console.WriteLine($"\t\tError:\n{log}");

                    try
                    {
                        File.Delete(outputFileTemp);
                    }
                    catch (Exception)
                    {
                    }

                    return;
                }

                try
                {
                    var texture = new SerializableTexture()
                    {
                        metadata = metadata,
                        data = File.ReadAllBytes(outputFileTemp),
                    };

                    if (metadata.keepOnCPU)
                    {
                        try
                        {
                            var data = File.ReadAllBytes(inputFile);

                            var rawData = Texture.LoadStandard(data, StandardTextureColorComponents.RGBA);

                            if (rawData != null)
                            {
                                texture.cpuData = new()
                                {
                                    colorComponents = rawData.colorComponents,
                                    data = rawData.data,
                                    width = rawData.width,
                                    height = rawData.height,
                                };
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }

                    var header = new SerializableTextureHeader();

                    using var stream = File.OpenWrite(outputFile);
                    using var writer = new BinaryWriter(stream);

                    var encoded = MessagePackSerializer.Serialize(header)
                        .Concat(MessagePackSerializer.Serialize(texture));

                    writer.Write(encoded.ToArray());
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to save baked texture: {e}");

                    try
                    {
                        if (replacedInput)
                        {
                            File.Delete(inputFile);
                        }

                        File.Delete(outputFileTemp);
                    }
                    catch (Exception)
                    {
                    }

                    return;
                }
                finally
                {
                    try
                    {
                        if (replacedInput)
                        {
                            File.Delete(inputFile);
                        }

                        File.Delete(outputFileTemp);
                    }
                    catch (Exception)
                    {
                    }
                }
            });
        }
    }
}
