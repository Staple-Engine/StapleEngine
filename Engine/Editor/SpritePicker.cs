using ImGuiNET;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.Editor
{
    internal static class SpritePicker
    {
        public static void Draw(ImGuiIOPtr io, ref bool showing, Texture texture, List<TextureSpriteInfo> sprites, Action<int> onFinish)
        {
            if(texture == null || sprites == null)
            {
                return;
            }

            var width = ImGui.GetContentRegionAvail().X;

            var aspect = texture.Width / (float)texture.Height;

            var height = width / aspect;

            var currentCursor = ImGui.GetCursorScreenPos();

            EditorGUI.Texture(texture, new Vector2(width, height));

            var scale = width / texture.Width;

            for(var i = 0; i < sprites.Count; i++)
            {
                var sprite = sprites[i];
                var spriteRect = sprite.originalRect.IsEmpty ? sprite.rect : sprite.originalRect;

                var position = new Vector2Int(Math.RoundToInt(currentCursor.X + spriteRect.left * scale),
                    Math.RoundToInt(currentCursor.Y + spriteRect.top * scale));

                var size = new Vector2Int(Math.RoundToInt(spriteRect.Width * scale), Math.RoundToInt(spriteRect.Height * scale));
                var rect = new Rect(position, size);

                ImGui.GetWindowDrawList().AddRect(new Vector2(rect.Min.X, rect.Min.Y),
                    new Vector2(rect.Max.X, rect.Max.Y), ImGuiProxy.ImGuiRGBA(255, 255, 255, 255));

                if(ImGui.IsMouseClicked(ImGuiMouseButton.Left) && rect.Contains(ImGui.GetMousePos()))
                {
                    showing = false;

                    onFinish?.Invoke(i);

                    return;
                }
            }
        }
    }
}
