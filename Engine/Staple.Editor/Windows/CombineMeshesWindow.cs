using Staple.Internal;
using System.IO;

namespace Staple.Editor;

internal class CombineMeshesWindow : EditorWindow
{
    private Transform targetContainer;

    [MenuItem("Tools/Mesh Combiner")]
    public static void ShowThis()
    {
        GetWindow<CombineMeshesWindow>();
    }

    public CombineMeshesWindow()
    {
        title = "Combine Meshes";

        windowFlags = EditorWindowFlags.Resizable;
    }

    public override void OnGUI()
    {
        targetContainer = (Transform)EditorGUI.ComponentField("Mesh Container", typeof(Transform), targetContainer, "CombineMeshesWindow.Container");

        EditorGUI.Disabled(!(targetContainer?.Entity.IsValid ?? false),
            () =>
            {
                EditorGUI.Button("Combine Mesh Renderers", "CombineMeshesWindow.Combine",
                    () =>
                    {
                        var data = MeshUtilities.CombineMeshes(targetContainer);

                        if(data == null || data.combinedMeshes.Count == 0)
                        {
                            EditorUtils.ShowMessageBox("The container had no valid Mesh Renderers", "Information", null);

                            return;
                        }

                        var serialized = data.ToAsset();

                        if(serialized == null || serialized.entries.Count == 0)
                        {
                            EditorUtils.ShowMessageBox("Failed to process the mesh data", "Error", null);

                            return;
                        }

                        serialized.Guid.Guid = GuidGenerator.Generate().ToString();

                        Platform.platformProvider.ShowSaveFileDialog("Save combined mesh data", "OK", "Cancel",
                            Path.Combine(StapleEditor.instance.BasePath, "Assets", "CombinedMeshAsset"),
                            new()
                            {
                                { "Staple Asset", "asset" }, { "All Files", "*" }
                            },
                            (result) =>
                            {
                                if (result.Length != 1)
                                {
                                    return;
                                }

                                if (!StapleEditor.SaveAsset(result[0], serialized))
                                {
                                    EditorUtils.ShowMessageBox("Failed to save the mesh data", "Error", null);
                                }
                            },
                            null);
                    });
            });
    }
}
