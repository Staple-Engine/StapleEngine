using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.Editor;

internal static class ImGuiUtils
{
    public class ContentGridItem
    {
        public bool renaming = false;
        public bool selected = false;
        public string renamedName;
        public string name;
        public Texture texture;
        public Func<Texture, Texture> ensureValidTexture;
    }

    /// <summary>
    /// Creates a content grid
    /// </summary>
    /// <param name="items">The items to show</param>
    /// <param name="padding">The padding</param>
    /// <param name="thumbnailSize">The thumbnail size</param>
    /// <param name="dragPayload">The drag payload, if any</param>
    /// <param name="onClick">Callback when an item is clicked</param>
    /// <param name="onDoubleClick">Callback when an item is double clicked</param>
    /// <param name="onDragDropped">Callback when an item was dropped</param>
    public static void ContentGrid(List<ContentGridItem> items, float padding, float thumbnailSize, string dragPayload, bool allowRename,
        Action<int, ContentGridItem> onClick, Action<int, ContentGridItem> onDoubleClick, Action<int, ContentGridItem> onDragDropped,
        Action<int, ContentGridItem, string> onRename, Action<int, ContentGridItem> onDelete)
    {
        var cellSize = padding + thumbnailSize;
        var width = ImGui.GetContentRegionAvail().X;

        var columnCount = (int)Math.Clamp((int)(width / cellSize), 1, int.MaxValue);

        ImGui.Columns(columnCount, "", false);

        for(var i = 0; i < items.Count; i++)
        {
            var index = i;
            var item = items[i];

            ImGui.PushID($"{item.name}##0");

            item.texture = item.ensureValidTexture(item.texture);

            ImGui.ImageButton("", ImGuiProxy.GetImGuiTexture(item.texture), new Vector2(thumbnailSize, thumbnailSize), new Vector2(0, 0), new Vector2(1, 1));

            if(ImGui.IsItemHovered())
            {
                if(ImGui.GetMouseClickedCount(ImGuiMouseButton.Left) == 2)
                {
                    onDoubleClick?.Invoke(i, item);
                }
                else if(Input.GetMouseButtonUp(MouseButton.Left))
                {
                    foreach(var j in items)
                    {
                        j.selected = false;
                        j.renaming = false;
                    }

                    item.selected = true;

                    onClick?.Invoke(i, item);
                }
                else if(dragPayload != null &&
                    StapleEditor.instance.dragDropPayloads.ContainsKey(dragPayload) == false &&
                    ImGui.BeginDragDropSource())
                {
                    unsafe
                    {
                        var buffer = System.Text.Encoding.UTF8.GetBytes(dragPayload);

                        fixed(byte *b = buffer)
                        {
                            ImGui.SetDragDropPayload(b, (void *)0, 0);
                        }
                    }

                    StapleEditor.instance.dragDropPayloads.AddOrSetKey(dragPayload, new StapleEditor.DragDropPayload()
                    {
                        index = index,
                        item = item,
                        action = onDragDropped,
                    });

                    ImGui.EndDragDropSource();
                }
                else if (Input.GetMouseButtonDown(MouseButton.Left) == false)
                {
                    StapleEditor.instance.dragDropPayloads.Clear();
                }
            }

            if (item.selected)
            {
                if (Input.GetKeyUp(KeyCode.Enter))
                {
                    item.renaming = true;
                    item.renamedName = item.name;
                }
                else if(Input.GetKeyUp(KeyCode.Delete))
                {
                    EditorUtils.ShowMessageBox($"Are you sure you want to delete {item.name}?", "OK", "Cancel",
                        () =>
                        {
                            onDelete?.Invoke(index, item);
                        },
                        null);
                }
            }

            if (item.renaming && Input.GetKeyDown(KeyCode.Escape) == false)
            {
                if (ImGui.InputText("##RENAME", ref item.renamedName, 1000, ImGuiInputTextFlags.EnterReturnsTrue |
                    ImGuiInputTextFlags.AutoSelectAll))
                {
                    item.renaming = false;

                    onRename?.Invoke(index, item, item.renamedName);
                }
            }
            else
            {
                item.renaming = false;

                ImGui.TextWrapped(item.name);
            }

            ImGui.NextColumn();

            ImGui.PopID();
        }

        ImGui.Columns(1);
    }
}
