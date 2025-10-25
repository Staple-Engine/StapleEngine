using Evergine.Bindings.KTX;
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

                var formatString = format switch
                {
                    TextureMetadataFormat.BC1 => "BC1_RGBA",
                    TextureMetadataFormat.BC2 => "BC2",
                    TextureMetadataFormat.BC3 => "BC3",
                    TextureMetadataFormat.BC4 => "BC4",
                    TextureMetadataFormat.BC5 => "BC5",
                    TextureMetadataFormat.BC6H => "BC6H",
                    TextureMetadataFormat.BC7 => "BC7",
                    TextureMetadataFormat.ASTC4x4 or TextureMetadataFormat.ASTC4x4F => "ASTC_4x4",
                    TextureMetadataFormat.ASTC5x4 or TextureMetadataFormat.ASTC5x4F => "ASTC_5x4",
                    TextureMetadataFormat.ASTC5x5 or TextureMetadataFormat.ASTC5x5F => "ASTC_4x5",
                    TextureMetadataFormat.ASTC6x5 or TextureMetadataFormat.ASTC6x5F => "ASTC_6x5",
                    TextureMetadataFormat.ASTC6x6 or TextureMetadataFormat.ASTC6x6F => "ASTC_6x6",
                    TextureMetadataFormat.ASTC8x5 or TextureMetadataFormat.ASTC8x5F => "ASTC_8x5",
                    TextureMetadataFormat.ASTC8x6 or TextureMetadataFormat.ASTC8x6F => "ASTC_8x6",
                    TextureMetadataFormat.ASTC8x8 or TextureMetadataFormat.ASTC8x8F => "ASTC_8x8",
                    TextureMetadataFormat.ASTC10x5 or TextureMetadataFormat.ASTC10x5F => "ASTC_10x5",
                    TextureMetadataFormat.ASTC10x6 or TextureMetadataFormat.ASTC10x6F => "ASTC_10x6",
                    TextureMetadataFormat.ASTC10x8 or TextureMetadataFormat.ASTC10x8F => "ASTC_10x8",
                    TextureMetadataFormat.ASTC10x10 or TextureMetadataFormat.ASTC10x10F => "ASTC_10x10",
                    TextureMetadataFormat.ASTC12x10 or TextureMetadataFormat.ASTC12x10F => "ASTC_12x10",
                    TextureMetadataFormat.ASTC12x12 or TextureMetadataFormat.ASTC12x12F => "ASTC_12x12",
                    TextureMetadataFormat.R8 or TextureMetadataFormat.R8I or TextureMetadataFormat.R8U or
                        TextureMetadataFormat.R8S => "R8",
                    TextureMetadataFormat.R16 or TextureMetadataFormat.R16I or TextureMetadataFormat.R16U or
                        TextureMetadataFormat.R16S or TextureMetadataFormat.R16F => "R16",
                    TextureMetadataFormat.RG8 or TextureMetadataFormat.RG8I or TextureMetadataFormat.RG8U or
                        TextureMetadataFormat.RG8S => "R8G8",
                    TextureMetadataFormat.RG16 or TextureMetadataFormat.RG16I or TextureMetadataFormat.RG16U or TextureMetadataFormat.RG16S or
                        TextureMetadataFormat.RG16F => "R16G16",
                    TextureMetadataFormat.BGRA8 => "B8G8R8A8",
                    TextureMetadataFormat.RGBA8 or TextureMetadataFormat.RGBA8I or TextureMetadataFormat.RGBA8U or
                        TextureMetadataFormat.RGBA8S => "R8G8B8A8",
                    TextureMetadataFormat.RGBA16 or TextureMetadataFormat.RGBA16I or TextureMetadataFormat.RGBA16U or
                        TextureMetadataFormat.RGBA16S or TextureMetadataFormat.RGBA16F => "R16G16B16A16",
                    TextureMetadataFormat.B5G6R5 => "B5G6R5",
                    TextureMetadataFormat.BGRA4 => "B4G4R4A4",
                    TextureMetadataFormat.BGR5A1 => "B5G5R5A1",
                    _ => "(invalid)"
                };

                var formatType = format.ToString() switch
                {
                    string s when s.EndsWith('I') => "int",
                    string s when s.EndsWith('U') => "uint",
                    string s when s.EndsWith('F') => "float",
                    string s when s.EndsWith('S') => "snorm",
                    _ => "unorm",
                };

                var qualityString = quality switch
                {
                    TextureMetadataQuality.Default => "normal",
                    TextureMetadataQuality.Fastest => "lowest",
                    TextureMetadataQuality.Highest => "highest",
                    _ => "normal",
                };

                var parameters = $"--format {formatString} --type {formatType} --quality {qualityString} --file-format ktx ";

                if (premultiplyAlpha)
                {
                    parameters += " --pre-multiply";
                }

                if (metadata.isLinear)
                {
                    parameters += " --srgb";
                }

                if (metadata.useMipmaps)
                {
                    parameters += " --mipmap";
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
                        Arguments = $"-i \"{inputFile}\" -o \"{outputFileTemp}\" {parameters}",
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
                    var fileData = File.ReadAllBytes(outputFileTemp);

                    var texture = new SerializableTexture()
                    {
                        metadata = metadata,
                    };

                    unsafe
                    {
                        fixed(byte *ptr = fileData)
                        {
                            ktxTexture* t = null;

                            var error = KTX.ktxTexture_CreateFromMemory(ptr, (nuint)fileData.Length,
                                (uint)ktxTextureCreateFlagBits.KTX_TEXTURE_CREATE_LOAD_IMAGE_DATA_BIT, &t);

                            if(error != ktx_error_code_e.KTX_SUCCESS)
                            {
                                Console.WriteLine($"\t\tError:\nFailed to load converted texture for {inputFile}: {error}");

                                File.Delete(outputFileTemp);

                                return;
                            }

                            var textureData = KTX.ktxTexture_GetData(t);
                            var size = KTX.ktxTexture_GetDataSize(t);
                            var width = t->baseWidth;
                            var height = t->baseHeight;

                            var finalData = new Span<byte>(textureData, (int)size).ToArray();

                            KTX.ktxTexture_Destroy(t);

                            texture.width = (int)width;
                            texture.height = (int)height;
                            texture.data = finalData;
                        }
                    }

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
