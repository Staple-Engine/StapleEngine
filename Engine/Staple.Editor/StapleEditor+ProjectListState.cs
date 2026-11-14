using Hexa.NET.ImGui;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Staple.Editor;

internal partial class StapleEditor
{
    public void HandleProjectListState(ImGuiIOPtr io)
    {
        ImGui.SetNextWindowSize(new Vector2(io.DisplaySize.X, io.DisplaySize.Y));

        ImGui.Begin("ProjectListContainer", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoDecoration);

        if(editorLogo != null)
        {
            EditorGUI.Texture(editorLogo, Vector2.One * 32);

            EditorGUI.SameLine();

            EditorGUI.HeaderLabel("Staple Engine Editor", 32);
        }

        ImGui.Text("Project List");

        ImGui.Spacing();

        if (ImGui.Button("New"))
        {
            ImGuiNewProject();
        }

        ImGui.SameLine();

        if (ImGui.Button("Open"))
        {
            ImGuiOpenProject();
        }

        ImGui.Spacing();

        var items = lastProjects.items.OrderByDescending(x => x.date).ToArray();

        var rowHeight = 64;

        EditorGUI.Table("##ProjectsList", items.Length, 4, true, null,
            (column) =>
            {
                return column switch
                {
                    0 => ("Icon", rowHeight),
                    1 => ("Name", 0),
                    2 => ("Path", 0),
                    3 => ("Date", 0),
                    _ => default,
                };
            },
            (row, column) =>
            {
                var item = items[row];

                switch (column)
                {
                    case 0: //Thumbnail

                        item.thumbnail ??= ThumbnailCache.GetTexture(Path.Combine(item.path, "Settings", "Icon.png"));

                        var thumbnail = item.thumbnail ?? projectBrowser.GetResourceIcon(ProjectBrowserResourceType.Other);

                        if(thumbnail != null)
                        {
                            EditorGUI.Texture(thumbnail, Vector2.One * rowHeight);
                        }

                        break;

                    case 1: //Name

                        EditorGUI.HeaderAligned(item.name, rowHeight, EditorGUI.TextAlignment.Middle, 26);

                        break;

                    case 2: //Path

                        EditorGUI.LabelAligned(item.path, rowHeight, EditorGUI.TextAlignment.Middle, 24);

                        break;

                    case 3: //Date

                        EditorGUI.LabelAligned(item.date.ToString(), rowHeight, EditorGUI.TextAlignment.Middle, 24);

                        break;
                }
            },
            (row) =>
            {
                if (!ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    return;
                }

                LoadProject(items[row].path);
            },
            rowHeight);

        ImGui.End();
    }
}
