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
        public TextureMetadata original;
        public string path;
        public string cachePath;
        public Texture previewTexture;

        private string[] textureMaxSizes = Array.Empty<string>();
        private long diskSize = 0;
        private uint VRAMSize = 0;

        public void UpdatePreview()
        {
            previewTexture?.Destroy();

            previewTexture = ResourceUtils.LoadTexture(cachePath);

            if(previewTexture == null)
            {
                return;
            }

            try
            {
                diskSize = new FileInfo(cachePath).Length;
            }
            catch(Exception)
            {
            }

            VRAMSize = previewTexture.info.storageSize;
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

            var hasChanges = metadata != original;

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
                EditorGUI.Label("Preview");

                var width = ImGui.GetContentRegionAvail().X;

                var aspect = previewTexture.Width / (float)previewTexture.Height;

                var height = width / aspect;

                var currentCursor = ImGui.GetCursorScreenPos();

                EditorGUI.Texture(previewTexture, new Vector2(width, height));

                var textureCursor = ImGui.GetCursorScreenPos();

                if(metadata.type == TextureType.Sprite)
                {
                    var scale = width / previewTexture.Width;

                    foreach (var sprite in previewTexture.metadata.sprites)
                    {
                        var position = new Vector2Int(Math.RoundToInt(currentCursor.X + sprite.rect.left * scale),
                            Math.RoundToInt(currentCursor.Y + sprite.rect.top * scale));

                        var size = new Vector2Int(Math.RoundToInt(sprite.rect.Width * scale), Math.RoundToInt(sprite.rect.Height * scale));
                        var rect = new Rect(position, size);

                        ImGui.GetWindowDrawList().AddRect(new Vector2(rect.Min.X, rect.Min.Y),
                            new Vector2(rect.Max.X, rect.Max.Y), ImGuiProxy.ImGuiRGBA(255, 255, 255, 255));
                    }
                }

                EditorGUI.Label($"{previewTexture.Width}x{previewTexture.Height}");
                EditorGUI.Label($"Disk Size: {EditorUtils.ByteSizeString(diskSize)}");
                EditorGUI.Label($"VRAM usage: {EditorUtils.ByteSizeString(VRAMSize)}");

                if(metadata.type == TextureType.Sprite)
                {
                    EditorGUI.Label($"{previewTexture.metadata.sprites.Count} sprites");
                }
            }
        }
    }
}
