using MessagePack;
using Newtonsoft.Json;
using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Baker
{
    static partial class Program
    {
        private static void ProcessTextures(AppPlatform platform, string texturecPath, string inputPath, string outputPath)
        {
            var textureFiles = new List<string>();

            foreach (var extension in textureExtensions)
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
                //Guid collision fix
                Thread.Sleep(25);

                Console.WriteLine($"\t{textureFiles[i].Replace(".meta", "")}");

                try
                {
                    if (File.Exists(textureFiles[i].Replace(".meta", "")) == false)
                    {
                        Console.WriteLine($"\t\tError: {textureFiles[i].Replace(".meta", "")} doesn't exist");

                        continue;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: {textureFiles[i].Replace(".meta", "")} doesn't exist");

                    continue;
                }

                var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(textureFiles[i]));
                var file = Path.GetFileName(textureFiles[i]);
                var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file.Replace(".meta", ""));

                var index = outputFile.IndexOf(inputPath);

                if (index >= 0 && index < outputFile.Length)
                {
                    outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
                }

                if (ShouldProcessFile(textureFiles[i], outputFile) == false &&
                    ShouldProcessFile(textureFiles[i].Replace(".meta", ""), outputFile.Replace(".meta", "")) == false)
                {
                    continue;
                }

                var inputFile = textureFiles[i].Replace(".meta", "");

                Console.WriteLine($"\t\t -> {outputFile}");

                string text;
                TextureMetadata metadata;

                try
                {
                    text = File.ReadAllText(textureFiles[i]);
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Failed to read file");

                    continue;
                }

                try
                {
                    metadata = JsonConvert.DeserializeObject<TextureMetadata>(text);
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Metadata is corrupted");

                    continue;
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

                if(metadata.overrides.TryGetValue(platform, out var overrides) && overrides.shouldOverride)
                {
                    format = overrides.format;
                    quality = overrides.quality;
                    maxSize = overrides.maxSize;
                    premultiplyAlpha = overrides.premultiplyAlpha;
                }

                RawTextureData textureData;

                try
                {
                    textureData = Texture.LoadStandard(File.ReadAllBytes(inputFile), StandardTextureColorComponents.RGBA);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tFailed to load image data");

                    continue;
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

                inputFile = "__temp";

                var png = textureData.EncodePNG();

                try
                {
                    File.WriteAllBytes(inputFile, png);
                }
                catch(Exception)
                {
                    Console.WriteLine("\t\tFailed to process: I/O Error");

                    continue;
                }

                metadata.sprites.Clear();

                if (metadata.type == TextureType.Sprite)
                {
                    var spriteTextures = new List<RawTextureData>();

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

                            if(scale != 1)
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
                                                    spriteTextures.Add(rawData);
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

                            var packed = Texture.PackTextures(spriteTextures.ToArray(), 32, 32, maxSize, metadata.padding, out var rects, out textureData);

                            if (packed)
                            {
                                foreach(var rect in rects)
                                {
                                    metadata.sprites.Add(new TextureSpriteInfo()
                                    {
                                        rect = rect,
                                        rotation = TextureSpriteRotation.None,
                                    });
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
                        Arguments = $"-f \"{inputFile}\" -o \"{outputFileTemp}\" {parameters} --validate",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                    }
                };

                process.Start();

                process.WaitForExit(300000);

                var result = "";

                while (!process.StandardOutput.EndOfStream)
                {
                    result += $"{process.StandardOutput.ReadLine()}\n";
                }

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"\t\tError:\n\t{result}\n");

                    try
                    {
                        File.Delete(outputFileTemp);
                    }
                    catch (Exception)
                    {
                    }

                    continue;
                }

                try
                {
                    var texture = new SerializableTexture()
                    {
                        metadata = metadata,
                        data = File.ReadAllBytes(outputFileTemp),
                    };

                    var header = new SerializableTextureHeader();

                    using (var stream = File.OpenWrite(outputFile))
                    {
                        using (var writer = new BinaryWriter(stream))
                        {
                            var encoded = MessagePackSerializer.Serialize(header)
                                .Concat(MessagePackSerializer.Serialize(texture));

                            writer.Write(encoded.ToArray());
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to save baked texture: {e}");

                    try
                    {
                        File.Delete(outputFileTemp);
                    }
                    catch (Exception)
                    {
                    }

                    continue;
                }
                finally
                {
                    try
                    {
                        File.Delete(outputFileTemp);
                        File.Delete(inputFile);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
    }
}
