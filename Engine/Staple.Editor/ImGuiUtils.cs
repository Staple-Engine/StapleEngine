using Hexa.NET.ImGui;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.Editor;

internal static class ImGuiUtils
{
    public abstract class ContentGridItem
    {
        public bool renaming = false;
        public bool selected = false;
        public string renamedName;
        public string name;
        public string ID;
        public Texture texture;

        public abstract Texture EnsureValidTexture(Texture existing);

        public abstract void NotVisible();

        public abstract void OnClick();

        public abstract void OnDoubleClick();

        public abstract void OnDragDropped();

        public abstract void OnRename(string name);

        public abstract void OnDelete();
    }

    /// <summary>
    /// Creates a content grid
    /// </summary>
    /// <param name="items">The items to show</param>
    /// <param name="padding">The padding</param>
    /// <param name="thumbnailSize">The thumbnail size</param>
    /// <param name="dragPayload">The drag payload, if any</param>
    /// <param name="allowRename">Whether the item may be renamed</param>
    /// <param name="allowDelete">Whether to allow deleting items</param>
    public static void ContentGrid(List<ContentGridItem> items, float padding, float thumbnailSize, string dragPayload, bool allowRename,
        bool allowDelete)
    {
        var cellSize = padding + thumbnailSize;
        var width = ImGui.GetContentRegionAvail().X;

        var columnCount = (int)Math.Clamp((int)(width / cellSize), 1, int.MaxValue);

        ImGui.Columns(columnCount, "", false);

        for(var i = 0; i < items.Count; i++)
        {
            var index = i;
            var item = items[i];

            ImGui.PushID($"{item.ID}##0");

            ImGui.ImageButton("", ImGuiProxy.GetImGuiTexture(item.texture), new Vector2(thumbnailSize, thumbnailSize), new Vector2(0, 0), new Vector2(1, 1));

            if(ImGui.IsItemVisible())
            {
                item.texture = item.EnsureValidTexture(item.texture);

                if (ImGui.IsItemHovered())
                {
                    if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    {
                        item.OnDoubleClick();
                    }
                    else if (Input.GetMouseButtonUp(MouseButton.Left))
                    {
                        foreach (var j in items)
                        {
                            j.selected = false;
                            j.renaming = false;
                        }

                        item.selected = true;

                        item.OnClick();
                    }
                    else if (dragPayload != null &&
                        !StapleEditor.instance.dragDropPayloads.ContainsKey(dragPayload) &&
                        ImGui.BeginDragDropSource())
                    {
                        unsafe
                        {
                            ImGui.SetDragDropPayload(dragPayload, null, 0);
                        }

                        StapleEditor.instance.dragDropPayloads.AddOrSetKey(dragPayload, new StapleEditor.DragDropPayload()
                        {
                            index = index,
                            item = item,
                            action = (index, _) => item.OnDragDropped(),
                        });

                        ImGui.EndDragDropSource();
                    }
                    else if (!Input.GetMouseButton(MouseButton.Left))
                    {
                        StapleEditor.instance.dragDropPayloads.Clear();
                    }
                }

                if (item.selected)
                {
                    if (Input.GetKeyUp(KeyCode.Enter) && allowRename)
                    {
                        item.renaming = true;
                        item.renamedName = item.name;
                    }
                    else if (Input.GetKeyUp(KeyCode.Delete) && allowDelete)
                    {
                        EditorUtils.ShowMessageBox($"Are you sure you want to delete {item.name}?", "OK", "Cancel",
                            () =>
                            {
                                item.OnDelete();
                            },
                            null);
                    }
                }

                if (item.renaming && !Input.GetKeyDown(KeyCode.Escape))
                {
                    if (ImGui.InputText("##RENAME", ref item.renamedName, 1000, ImGuiInputTextFlags.EnterReturnsTrue |
                        ImGuiInputTextFlags.AutoSelectAll))
                    {
                        item.renaming = false;

                        item.OnRename(item.renamedName);
                    }
                }
                else
                {
                    item.renaming = false;

                    ImGui.Text(item.name);
                }
            }
            else
            {
                item.NotVisible();
            }

            ImGui.NextColumn();

            ImGui.PopID();
        }

        ImGui.Columns(1);
    }
}
