using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.Editor
{
    internal static class ImGuiUtils
    {
        public class ContentGridItem
        {
            public string name;
            public Texture texture;
            public Func<Texture, Texture> ensureValidTexture;
        }

        public static void ContentGrid(List<ContentGridItem> items, float padding, float thumbnailSize,
            Action<int, ContentGridItem> onClick, Action<int, ContentGridItem> onDoubleClick)
        {
            var cellSize = padding + thumbnailSize;
            var width = ImGui.GetContentRegionAvail().X;

            var columnCount = (int)Math.Clamp((int)(width / cellSize), 1, int.MaxValue);

            ImGui.Columns(columnCount, "", false);

            for(var i = 0; i < items.Count; i++)
            {
                var item = items[i];

                ImGui.PushID($"{item.name}##0");

                item.texture = item.ensureValidTexture(item.texture);

                ImGui.ImageButton("", ImGuiProxy.GetImGuiTexture(item.texture), new Vector2(thumbnailSize, thumbnailSize), new Vector2(0, 0), new Vector2(1, 1));

                if(ImGui.IsItemHovered())
                {
                    if(ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    {
                        onDoubleClick?.Invoke(i, item);
                    }
                    else if(ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        onClick?.Invoke(i, item);
                    }
                }

                ImGui.TextWrapped(item.name);

                ImGui.NextColumn();

                ImGui.PopID();
            }

            ImGui.Columns(1);
        }
    }
}
