using ImGuiNET;
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

namespace Staple.Editor
{
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

        public void UpdatePreview()
        {
            var metadata = target as TextureMetadata;

            previewTexture?.Destroy();
            originalTexture?.Destroy();

            ThumbnailCache.ClearSingle(path.Replace(".meta", ""));

            previewTexture = ResourceUtils.LoadTexture(cachePath);
            originalTexture = metadata.shouldPack ? ThumbnailCache.GetTexture(path.Replace(".meta", "")) : null;

            if(previewTexture == null)
            {
                return;
            }

            try
            {
                diskSize = new FileInfo(cachePath).Length;
                originalDiskSize = new FileInfo(path.Replace(".meta", "")).Length;
            }
            catch(Exception)
            {
            }

            VRAMSize = previewTexture.info.storageSize;
            originalVRAMSize = originalTexture?.info.storageSize ?? 0;
        }

        public override bool RenderField(FieldInfo field)
        {
            var metadata = target as TextureMetadata;

            if (field.Name.StartsWith("sprite") && metadata.type != TextureType.Sprite)
            {
                return true;
            }

            switch(field.Name)
            {
                case nameof(TextureMetadata.guid):
                case nameof(TextureMetadata.sprites):

                    return true;

                case nameof(TextureMetadata.padding):
                case nameof(TextureMetadata.trimDuplicates):

                    return metadata.shouldPack == false;

                case nameof(TextureMetadata.maxSize):

                    {
                        var current = (int)field.GetValue(target);

                        var index = Array.IndexOf(TextureMetadata.TextureMaxSizes, current);

                        if (textureMaxSizes.Length == 0)
                        {
                            textureMaxSizes = TextureMetadata.TextureMaxSizes.Select(x => x.ToString()).ToArray();
                        }

                        var newIndex = EditorGUI.Dropdown(field.Name.ExpandCamelCaseName(), textureMaxSizes, index);

                        if (index != newIndex)
                        {
                            field.SetValue(target, TextureMetadata.TextureMaxSizes[newIndex]);
                        }

                        return true;
                    }

                case nameof(TextureMetadata.overrides):

                    if (ImGui.BeginTabBar($"##{metadata.guid}_OVERRIDES"))
                    {
                        var platformTypes = Enum.GetValues<AppPlatform>();

                        foreach (var platform in platformTypes)
                        {
                            if (ImGui.BeginTabItem(platform.ToString()))
                            {
                                var overrides = metadata.overrides;

                                if (overrides.TryGetValue(platform, out var item) == false)
                                {
                                    item = new();

                                    overrides.Add(platform, item);
                                }

                                item.shouldOverride = EditorGUI.Toggle("Override", item.shouldOverride);

                                if (item.shouldOverride == false)
                                {
                                    ImGui.BeginDisabled();
                                }

                                var format = item.shouldOverride ? item.format : metadata.format;

                                var quality = item.shouldOverride ? item.quality : metadata.quality;

                                var maxSize = item.shouldOverride ? item.maxSize : metadata.maxSize;

                                var premultiplyAlpha = item.shouldOverride ? item.premultiplyAlpha : metadata.premultiplyAlpha;

                                format = EditorGUI.EnumDropdown("Format", format);

                                quality = EditorGUI.EnumDropdown("Quality", quality);

                                var index = Array.IndexOf(TextureMetadata.TextureMaxSizes, maxSize);

                                if (textureMaxSizes.Length == 0)
                                {
                                    textureMaxSizes = TextureMetadata.TextureMaxSizes.Select(x => x.ToString()).ToArray();
                                }

                                var newIndex = EditorGUI.Dropdown("Max Size", textureMaxSizes, index);

                                if (index != newIndex)
                                {
                                    maxSize = TextureMetadata.TextureMaxSizes[newIndex];
                                }

                                premultiplyAlpha = EditorGUI.Toggle("Premultiply Alpha", premultiplyAlpha);

                                if (item.shouldOverride == false)
                                {
                                    ImGui.EndDisabled();
                                }
                                else
                                {
                                    item.format = format;
                                    item.quality = quality;
                                    item.maxSize = maxSize;
                                    item.premultiplyAlpha = premultiplyAlpha;
                                }

                                ImGui.EndTabItem();
                            }
                        }

                        ImGui.EndTabBar();
                    }

                    return true;
            }

            return base.RenderField(field);
        }

        public override void OnInspectorGUI ()
        {
            base.OnInspectorGUI();

            var metadata = (TextureMetadata)target;
            var originalMetadata = (TextureMetadata)original;

            var hasChanges = metadata != originalMetadata;

            if(hasChanges)
            {
                if (EditorGUI.Button("Apply"))
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
                }

                EditorGUI.SameLine();

                if (EditorGUI.Button("Revert"))
                {
                    var fields = metadata.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

                    foreach (var field in fields)
                    {
                        field.SetValue(metadata, field.GetValue(original));
                    }
                }
            }
            else
            {
                EditorGUI.ButtonDisabled("Apply");

                EditorGUI.SameLine();

                EditorGUI.ButtonDisabled("Revert");
            }

            if(EditorGUI.Button("Apply to all in folder"))
            {
                try
                {
                    var files = Directory.GetFiles(Path.GetDirectoryName(path), "*.meta");

                    foreach(var file in files)
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
                        catch(Exception)
                        {
                            newMetadata.guid = Guid.NewGuid().ToString();
                        }

                        var texture = ThumbnailCache.GetThumbnail(file.Replace(".meta", ""));

                        if(texture != null)
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
                catch(Exception e)
                {
                    Log.Error($"Failed to Apply All: {e}");
                }
            }

            if (previewTexture != null)
            {
                void DrawTexture(Texture texture, long diskSize, uint VRAMSize, List<TextureSpriteInfo> sprites, bool isOriginal)
                {
                    var width = ImGui.GetContentRegionAvail().X;

                    var aspect = texture.Width / (float)texture.Height;

                    var height = width / aspect;

                    var currentCursor = ImGui.GetCursorScreenPos();

                    EditorGUI.Texture(texture, new Vector2(width, height));

                    var textureCursor = ImGui.GetCursorScreenPos();

                    if (metadata.type == TextureType.Sprite)
                    {
                        var scale = width / texture.Width;

                        foreach (var sprite in sprites)
                        {
                            if(isOriginal == false && sprite.rotation != TextureSpriteRotation.None)
                            {
                                continue;
                            }

                            var spriteRect = isOriginal ? sprite.originalRect : sprite.rect;

                            var position = new Vector2Int(Math.RoundToInt(currentCursor.X + spriteRect.left * scale),
                                Math.RoundToInt(currentCursor.Y + spriteRect.top * scale));

                            var size = new Vector2Int(Math.RoundToInt(spriteRect.Width * scale), Math.RoundToInt(spriteRect.Height * scale));
                            var rect = new Rect(position, size);

                            ImGui.GetWindowDrawList().AddRect(new Vector2(rect.Min.X, rect.Min.Y),
                                new Vector2(rect.Max.X, rect.Max.Y), ImGuiProxy.ImGuiRGBA(255, 255, 255, 255));

                            if(isOriginal)
                            {
                                void CenteredText(string text)
                                {
                                    Vector2 textSize = ImGui.CalcTextSize(text);

                                    ImGui.GetWindowDrawList().AddText(new Vector2(rect.Min.X + (rect.Width - textSize.X) / 2,
                                        rect.Min.Y + (rect.Height - textSize.Y) / 2),
                                        ImGuiProxy.ImGuiRGBA(255, 255, 255, 255), text);
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
                    if (ImGui.BeginTabBar("##TextureAssetPreviewTexture"))
                    {
                        if (ImGui.BeginTabItem("Preview"))
                        {
                            DrawTexture(originalTexture, originalDiskSize, originalVRAMSize, previewTexture.metadata.sprites, true);

                            ImGui.EndTabItem();
                        }

                        if(ImGui.BeginTabItem("Processed"))
                        {
                            DrawTexture(previewTexture, diskSize, VRAMSize, previewTexture.metadata.sprites, false);

                            ImGui.EndTabItem();
                        }

                        ImGui.EndTabBar();
                    }
                }
                else if(previewTexture != null)
                {
                    EditorGUI.Label("Preview:");

                    DrawTexture(previewTexture, diskSize, VRAMSize, previewTexture.metadata.sprites, false);
                }
            }
        }
    }
}
