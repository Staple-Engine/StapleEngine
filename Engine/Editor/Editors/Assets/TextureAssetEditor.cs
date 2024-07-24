using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;

namespace Staple.Editor;

[CustomEditor(typeof(TextureMetadata))]
internal class TextureAssetEditor : Editor
{
    public Texture previewTexture;
    public Texture originalTexture;

    private string[] textureMaxSizes = Array.Empty<string>();
    private long diskSize = 0;
    private long originalDiskSize = 0;
    private uint VRAMSize = 0;
    private uint originalVRAMSize = 0;
    private bool initialized = false;

    public void UpdatePreview()
    {
        var metadata = target as TextureMetadata;

        previewTexture?.Destroy();
        originalTexture?.Destroy();

        ThumbnailCache.ClearSingle(path.Replace(".meta", ""));

        previewTexture = ResourceUtils.LoadTexture(cachePath);
        originalTexture = metadata.shouldPack ? ThumbnailCache.GetTexture(path.Replace(".meta", "")) : null;

        if (previewTexture == null)
        {
            return;
        }

        try
        {
            diskSize = new FileInfo(cachePath).Length;
            originalDiskSize = new FileInfo(path.Replace(".meta", "")).Length;
        }
        catch (Exception)
        {
        }

        VRAMSize = previewTexture.info.storageSize;
        originalVRAMSize = originalTexture?.info.storageSize ?? 0;
    }

    public override bool DrawProperty(Type fieldType, string name, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        var metadata = target as TextureMetadata;

        if (name.StartsWith("sprite") && metadata.type != TextureType.Sprite)
        {
            return true;
        }

        switch (name)
        {
            case nameof(TextureMetadata.padding):
            case nameof(TextureMetadata.trimDuplicates):

                return metadata.shouldPack == false;

            case nameof(TextureMetadata.maxSize):

                {
                    var current = (int)getter();

                    var index = Array.IndexOf(TextureMetadata.TextureMaxSizes, current);

                    if (textureMaxSizes.Length == 0)
                    {
                        textureMaxSizes = TextureMetadata.TextureMaxSizes.Select(x => x.ToString()).ToArray();
                    }

                    var newIndex = EditorGUI.Dropdown(name.ExpandCamelCaseName(), "TextureMetadataMaxSize", textureMaxSizes, index);

                    if (index != newIndex)
                    {
                        setter(TextureMetadata.TextureMaxSizes[newIndex]);
                    }

                    return true;
                }

            case nameof(TextureMetadata.overrides):

                {
                    var platformTypes = Enum.GetValues<AppPlatform>();

                    EditorGUI.TabBar(platformTypes.Select(x => x.ToString()).ToArray(), "TextureMetadataOverrides", (tabIndex) =>
                    {
                        var platform = platformTypes[tabIndex];
                        var overrides = metadata.overrides;

                        if (overrides.TryGetValue(platform, out var item) == false)
                        {
                            item = new();

                            overrides.Add(platform, item);
                        }

                        item.shouldOverride = EditorGUI.Toggle("Override", $"TextureMetadataOverride{tabIndex}Override", item.shouldOverride);

                        EditorGUI.Disabled(item.shouldOverride == false, () =>
                        {
                            var format = item.shouldOverride ? item.format : metadata.format;

                            var quality = item.shouldOverride ? item.quality : metadata.quality;

                            var maxSize = item.shouldOverride ? item.maxSize : metadata.maxSize;

                            var premultiplyAlpha = item.shouldOverride ? item.premultiplyAlpha : metadata.premultiplyAlpha;

                            format = EditorGUI.EnumDropdown("Format", $"TextureMetadataOverride{tabIndex}Format", format);

                            quality = EditorGUI.EnumDropdown("Quality", $"TextureMetadataOverride{tabIndex}Quality", quality);

                            var index = Array.IndexOf(TextureMetadata.TextureMaxSizes, maxSize);

                            if (textureMaxSizes.Length == 0)
                            {
                                textureMaxSizes = TextureMetadata.TextureMaxSizes.Select(x => x.ToString()).ToArray();
                            }

                            var newIndex = EditorGUI.Dropdown("Max Size", $"TextureMetadataOverride{tabIndex}MaxSize", textureMaxSizes, index);

                            if (index != newIndex)
                            {
                                maxSize = TextureMetadata.TextureMaxSizes[newIndex];
                            }

                            premultiplyAlpha = EditorGUI.Toggle("Premultiply Alpha", $"TextureMetadataOverride{tabIndex}PremultiplyAlpha", premultiplyAlpha);

                            if (item.shouldOverride)
                            {
                                item.format = format;
                                item.quality = quality;
                                item.maxSize = maxSize;
                                item.premultiplyAlpha = premultiplyAlpha;
                            }
                        });
                    });
                }

                return true;
        }

        return false;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var metadata = (TextureMetadata)target;
        var originalMetadata = (TextureMetadata)original;

        if(initialized == false)
        {
            initialized = true;

            UpdatePreview();
        }

        var hasChanges = metadata != originalMetadata;

        if (hasChanges)
        {
            EditorGUI.Button("Apply", "TextureMetadataApply", () =>
            {
                try
                {
                    var text = JsonConvert.SerializeObject(metadata, Formatting.Indented, new JsonSerializerSettings()
                    {
                        Converters =
                        {
                            new StringEnumConverter(),
                        }
                    });

                    File.WriteAllText(path, text);
                }
                catch (Exception)
                {
                }

                var fields = metadata.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

                foreach (var field in fields)
                {
                    field.SetValue(original, field.GetValue(metadata));
                }

                EditorUtils.RefreshAssets(false, UpdatePreview);
            });

            EditorGUI.SameLine();

            EditorGUI.Button("Revert", "TextureMetadataRevert", () =>
            {
                var fields = metadata.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

                foreach (var field in fields)
                {
                    field.SetValue(metadata, field.GetValue(original));
                }
            });
        }
        else
        {
            EditorGUI.ButtonDisabled("Apply", "TextureMetadataApply", null);

            EditorGUI.SameLine();

            EditorGUI.ButtonDisabled("Revert", "TextureMetadataRevert", null);
        }

        EditorGUI.Button("Apply to all in folder", "TextureMetadataApplyAll", () =>
        {
            try
            {
                var files = Directory.GetFiles(Path.GetDirectoryName(path), "*.meta");

                foreach (var file in files)
                {
                    //Guid uniqueness fix
                    Thread.Sleep(25);

                    var newMetadata = metadata.Clone();

                    try
                    {
                        var data = File.ReadAllText(file);

                        var existing = JsonConvert.DeserializeObject<TextureMetadata>(data);

                        newMetadata.guid = existing.guid;
                    }
                    catch (Exception)
                    {
                        newMetadata.guid = Guid.NewGuid().ToString();
                    }

                    var texture = ThumbnailCache.GetThumbnail(file.Replace(".meta", ""));

                    if (texture != null)
                    {
                        var json = JsonConvert.SerializeObject(newMetadata, Formatting.Indented, new JsonSerializerSettings()
                        {
                            Converters =
                            {
                                new StringEnumConverter(),
                            }
                        });

                        File.WriteAllText(file, json);
                    }
                }

                EditorUtils.RefreshAssets(false, UpdatePreview);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to Apply All: {e}");
            }
        });

        if (previewTexture != null)
        {
            void DrawTexture(Texture texture, long diskSize, uint VRAMSize, List<TextureSpriteInfo> sprites, bool isOriginal)
            {
                var width = EditorGUI.RemainingHorizontalSpace();

                var aspect = texture.Width / (float)texture.Height;

                var height = width / aspect;

                var currentCursor = EditorGUI.CurrentGUICursorPosition();

                EditorGUI.Texture(texture, new Vector2(width, height));

                var textureCursor = EditorGUI.CurrentGUICursorPosition();

                if (metadata.type == TextureType.Sprite)
                {
                    var scale = width / texture.Width;

                    foreach (var sprite in sprites)
                    {
                        if (isOriginal == false && sprite.rotation != TextureSpriteRotation.None)
                        {
                            continue;
                        }

                        var spriteRect = isOriginal ? sprite.originalRect : sprite.rect;

                        var position = new Vector2Int(Math.RoundToInt(currentCursor.X + spriteRect.left * scale),
                            Math.RoundToInt(currentCursor.Y + spriteRect.top * scale));

                        var size = new Vector2Int(Math.RoundToInt(spriteRect.Width * scale), Math.RoundToInt(spriteRect.Height * scale));
                        var rect = new Rect(position, size);

                        EditorGUI.AddRectangle(rect, Color32.White);

                        if (isOriginal)
                        {
                            void CenteredText(string text)
                            {
                                Vector2 textSize = EditorGUI.GetTextSize(text);

                                EditorGUI.AddText(text, new Vector2(rect.Min.X + (rect.Width - textSize.X) / 2,
                                    rect.Min.Y + (rect.Height - textSize.Y) / 2), Color32.White);
                            }

                            switch (sprite.rotation)
                            {
                                case TextureSpriteRotation.FlipX:

                                    CenteredText("X");

                                    break;

                                case TextureSpriteRotation.FlipY:

                                    CenteredText("Y");

                                    break;

                                case TextureSpriteRotation.Duplicate:

                                    CenteredText("D");

                                    break;

                                case TextureSpriteRotation.None:

                                    break;
                            }
                        }
                    }
                }

                EditorGUI.Label($"{texture.Width}x{texture.Height}");
                EditorGUI.Label($"Disk Size: {EditorUtils.ByteSizeString(diskSize)}");
                EditorGUI.Label($"VRAM usage: {EditorUtils.ByteSizeString(VRAMSize)}");

                if (metadata.type == TextureType.Sprite)
                {
                    EditorGUI.Label($"{texture.metadata.sprites.Count} sprites");
                }
            }

            if (previewTexture != null && previewTexture.Disposed == false &&
                originalTexture != null && originalTexture.Disposed == false)
            {
                EditorGUI.TabBar(["Preview", "Processed"], "TextureMetadataPreview", (tabIndex) =>
                {
                    switch(tabIndex)
                    {
                        case 0:
                            DrawTexture(originalTexture, originalDiskSize, originalVRAMSize, previewTexture.metadata.sprites, true);

                            break;

                        case 1:

                            DrawTexture(previewTexture, diskSize, VRAMSize, previewTexture.metadata.sprites, false);

                            break;
                    }
                });
            }
            else if (previewTexture != null)
            {
                EditorGUI.Label("Preview:");

                DrawTexture(previewTexture, diskSize, VRAMSize, previewTexture.metadata.sprites, false);
            }
        }
    }
}
