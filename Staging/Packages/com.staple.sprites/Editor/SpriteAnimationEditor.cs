using Staple.Internal;
using System;
using System.IO;
using System.Numerics;

namespace Staple.Editor;

[CustomEditor(typeof(SpriteAnimation))]
public class SpriteAnimationEditor : StapleAssetEditor
{
    private int currentFrame = 0;
    private float timer = 0.0f;

    public override bool DrawProperty(Type fieldType, string name, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        var asset = (SpriteAnimation)target;

        if(name == nameof(SpriteAnimation.frames))
        {
            if(asset.texture != null)
            {
                EditorGUI.Button("+", $"SpriteAnimationFramesAdd", () =>
                {
                    asset.frames.Add(0);
                });

                EditorGUI.Group(() =>
                {
                    for (var i = 0; i < asset.frames.Count; i++)
                    {
                        var frame = asset.frames[i];
                        var frameIndex = i;

                        EditorGUI.Button("-", $"SpriteAnimationFrames{i}Remove", () =>
                        {
                            asset.frames.RemoveAt(i);
                        });

                        EditorGUI.SameLine();

                        if (frame >= 0 && frame < asset.texture.Sprites.Length)
                        {
                            var sprite = asset.texture.Sprites[frame];

                            EditorGUI.TextureRect(asset.texture, sprite.Rect, new Vector2(32, 32), sprite.Rotation);
                        }
                        else
                        {
                            EditorGUI.Label("(none)");
                        }

                        EditorGUI.SameLine();

                        EditorGUI.Button("O", $"SpriteAnimationFrames{i}Browse", () =>
                        {
                            var assetPath = AssetSerialization.GetAssetPathFromCache(AssetDatabase.GetAssetPath(asset.texture.Guid.Guid));

                            if (assetPath != asset.texture.Guid.Guid && Path.IsPathRooted(assetPath) == false)
                            {
                                assetPath = $"Assets{Path.DirectorySeparatorChar}{assetPath}";
                            }

                            EditorGUI.ShowSpritePicker(asset.texture, (index) => asset.frames[frameIndex] = index);
                        });
                    }
                });
            }

            return true;
        }

        return false;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var asset = (SpriteAnimation)target;

        if(asset.texture != null && asset.frames.Count > 0)
        {
            if(asset.frameRate <= 0)
            {
                asset.frameRate = 1;
            }

            timer += Time.unscaledDeltaTime;

            if(currentFrame >= asset.frames.Count)
            {
                currentFrame = 0;
            }

            var timeout = asset.frameRateIsMilliseconds ? asset.frameRate / 1000.0f : 1000.0f / asset.frameRate / 1000.0f;

            while(timer >= timeout)
            {
                timer -= timeout;

                currentFrame++;

                if(currentFrame >= asset.frames.Count)
                {
                    currentFrame = 0;
                }
            }

            var frame = asset.frames[currentFrame];

            if(frame < 0 || frame >= asset.texture.Sprites.Length)
            {
                return;
            }

            var sprite = asset.texture.Sprites[frame];

            var width = EditorGUI.RemainingHorizontalSpace();

            var aspect = sprite.Rect.Width / (float)sprite.Rect.Height;

            var height = width / aspect;

            EditorGUI.TextureRect(asset.texture, sprite.Rect, new Vector2(width, height), sprite.Rotation);
        }
    }
}
