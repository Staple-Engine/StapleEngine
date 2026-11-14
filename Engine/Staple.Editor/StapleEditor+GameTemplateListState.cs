using Hexa.NET.ImGui;
using Newtonsoft.Json;
using Staple.Internal;
using Staple.ProjectManagement;
using System.IO;
using System.Numerics;

namespace Staple.Editor;

internal partial class StapleEditor
{
    private const float rightPanelWidth = 400;
    private const float templateItemWidth = 150;

    public void HandleGameTemplateListState(ImGuiIOPtr io)
    {
        ImGui.SetNextWindowSize(new Vector2(io.DisplaySize.X, io.DisplaySize.Y));

        ImGui.Begin("ProjectListContainer", ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoDocking |
            ImGuiWindowFlags.NoDecoration);

        if (editorLogo != null)
        {
            EditorGUI.Texture(editorLogo, Vector2.One * 32);

            EditorGUI.SameLine();

            EditorGUI.HeaderLabel("Staple Engine - Project Templates", 32);
        }

        EditorGUI.Space();

        {
            var buttonSize = EditorGUI.MeasureLabel("Back").X * 2;

            ImGui.SetCursorPosX(io.DisplaySize.X - buttonSize);

            EditorGUI.Button("Back", "GameTemplateList.Back", () =>
            {
                editorState = EditorState.ProjectList;
            });
        }

        EditorGUI.Space();

        EditorGUI.WindowFrame($"GameTemplateList.TemplateList.Frame", new(io.DisplaySize.X - rightPanelWidth, 0),
            () =>
            {
                var columns = Math.FloorToInt(ImGui.GetContentRegionAvail().X / (templateItemWidth + 40));

                ImGui.Columns(columns, false);

                var counter = 0;

                foreach (var template in ProjectTemplateManager.Instance.templates)
                {
                    void SelectTemplate()
                    {
                        if (ImGui.IsItemHovered() && Input.GetMouseButtonUp(MouseButton.Left))
                        {
                            selectedGameTemplate = template;
                        }
                    }

                    if (template.iconHolder != null)
                    {
                        var aspect = template.iconHolder.Height / (float)template.iconHolder.Width;

                        EditorGUI.Texture(template.iconHolder, new(templateItemWidth, templateItemWidth * aspect));
                    }

                    SelectTemplate();

                    var labelSize = EditorGUI.MeasureHeaderLabel(template.manifest.name, 26).X;

                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (templateItemWidth - labelSize) / 2);

                    EditorGUI.HeaderLabel(template.manifest.name, 26);

                    SelectTemplate();

                    ImGui.NextColumn();

                    counter++;
                }

                ImGui.Columns(1);
            });

        EditorGUI.SameLine();

        EditorGUI.WindowFrame($"GameTemplateList.TemplateDetails.Frame", new(rightPanelWidth, 0),
            () =>
            {
                if (selectedGameTemplate == null)
                {
                    var title = "No template selected";
                    var size = 30;

                    var labelSize = EditorGUI.MeasureHeaderLabel(title, size).X;

                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetContentRegionAvail().X - labelSize) / 2);

                    EditorGUI.HeaderAligned(title, ImGui.GetContentRegionAvail().Y, EditorGUI.TextAlignment.Middle, size);

                    return;
                }
                else
                {
                    //For some reason need a LOT of spaces
                    EditorGUI.Space();
                    EditorGUI.Space();
                    EditorGUI.Space();
                    EditorGUI.Space();
                    EditorGUI.Space();

                    var manifest = selectedGameTemplate.manifest;

                    if (selectedGameTemplate.iconHolder != null)
                    {
                        EditorGUI.CenterHorizontally(128, ImGui.GetContentRegionAvail().X, true);

                        EditorGUI.Texture(selectedGameTemplate.iconHolder, Vector2.One * 128);
                    }

                    var title = manifest.name;
                    var size = 30;

                    var labelSize = EditorGUI.MeasureHeaderLabel(title, size).X;

                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetContentRegionAvail().X - labelSize) / 2);

                    EditorGUI.HeaderLabel(title, size);

                    ImGui.SetCursorPosX(20);

                    ImGui.PushTextWrapPos(ImGui.GetContentRegionAvail().X - 10);

                    ImGui.Text(manifest.description);

                    ImGui.PopTextWrapPos();

                    ImGui.SetCursorPosX(20);

                    EditorGUI.HeaderLabel("Author: ");

                    EditorGUI.SameLine();

                    EditorGUI.Label(string.IsNullOrEmpty(manifest.author) ? "(unknown)" : manifest.author);

                    ImGui.SetCursorPosX(20);

                    EditorGUI.HeaderLabel("License: ");

                    EditorGUI.SameLine();

                    EditorGUI.Label(string.IsNullOrEmpty(manifest.license) ? "(unknown)" : manifest.license);

                    EditorGUI.Space();
                    EditorGUI.Space();
                    EditorGUI.Space();

                    ImGui.SetCursorPosX(20);

                    gameTemplateProjectName = EditorGUI.TextField("Project Name", "GameTemplateList.TemplateDetails.ProjectName", gameTemplateProjectName);

                    ImGui.SetCursorPosX(20);

                    gameTemplateCompanyName = EditorGUI.TextField("Company Name", "GameTemplateList.TemplateDetails.CompanyName",
                        gameTemplateCompanyName);

                    ImGui.SetCursorPosX(20);

                    gameTemplateTargetFolder = EditorGUI.TextField("Project Folder", "GameTemplateList.TemplateDetails.ProjectName", gameTemplateTargetFolder);

                    ImGui.SetCursorPosX(20);

                    EditorGUI.Button("Browse", "GameTemplateList.TemplateDetails.ProjectName.Browse",
                        () =>
                        {
                            Platform.platformProvider.ShowOpenFolderDialog("Create Project", "OK", "Cancel", "", false,
                                (result) =>
                                {
                                    if (result.Length == 0)
                                    {
                                        return;
                                    }

                                    gameTemplateTargetFolder = result[0];
                                },
                                null);
                        });

                    bool IsValidTargetFolder()
                    {
                        if(string.IsNullOrEmpty(gameTemplateTargetFolder))
                        {
                            return false;
                        }

                        try
                        {
                            if(!Directory.Exists(gameTemplateTargetFolder) ||
                                Directory.GetFiles(gameTemplateTargetFolder).Length != 0 ||
                                Directory.GetDirectories(gameTemplateTargetFolder).Length != 0)
                            {
                                return false;
                            }

                            return true;
                        }
                        catch(System.Exception)
                        {
                            return false;
                        }
                    }

                    var errorMessage = "";

                    void AddError(string message)
                    {
                        if(string.IsNullOrEmpty(errorMessage))
                        {
                            errorMessage = message;

                            return;
                        }

                        errorMessage += $"\n{message}";
                    }

                    if(!IsValidTargetFolder())
                    {
                        AddError("• Invalid project folder");
                    }

                    if(string.IsNullOrWhiteSpace(gameTemplateCompanyName))
                    {
                        AddError("• Invalid company name");
                    }

                    if(string.IsNullOrWhiteSpace(gameTemplateProjectName))
                    {
                        AddError("• Invalid project name");
                    }

                    EditorGUI.Disabled(!string.IsNullOrEmpty(errorMessage),
                        () =>
                        {
                            ImGui.SetCursorPosX(20);

                            EditorGUI.Button("Create", "GameTemplateList.TemplateDetails.ProjectName.Create",
                                () =>
                                {
                                    var appSettings = AppSettings.Default;

                                    appSettings.appName = gameTemplateProjectName;
                                    appSettings.companyName = gameTemplateCompanyName;

                                    string NormalizeBundlePart(string part)
                                    {
                                        var pieces = part.Split(' ');

                                        for (var i = 0; i < pieces.Length; i++)
                                        {
                                            pieces[i] = pieces[i].ToLowerInvariant();
                                        }

                                        return string.Concat(pieces);
                                    }

                                    var bundleCompanyName = NormalizeBundlePart(gameTemplateCompanyName);
                                    var bundleName = NormalizeBundlePart(gameTemplateProjectName);

                                    appSettings.appBundleID = $"com.{bundleCompanyName}.{bundleName}";

                                    try
                                    {
                                        var copy = appSettings.Clone();

                                        if(!string.IsNullOrEmpty(selectedGameTemplate.additionalSettingsContent))
                                        {
                                            JsonConvert.PopulateObject(selectedGameTemplate.additionalSettingsContent, copy);

                                            appSettings = copy;
                                        }
                                    }
                                    catch(System.Exception)
                                    {
                                    }

                                    if(!CreateProject(gameTemplateTargetFolder))
                                    {
                                        ShowMessageBox("Failed to create project! Please ensure the folder exists and you have proper permissions!",
                                            "OK", null);

                                        return;
                                    }

                                    var contentsPath = Path.Combine(selectedGameTemplate.basePath, "Contents");
                                    var targetContentsPath = Path.Combine(gameTemplateTargetFolder, "Assets");
                                    var iconPath = Path.Combine(selectedGameTemplate.basePath, "Icon.png");
                                    var targetIconPath = Path.Combine(gameTemplateTargetFolder, "Settings", "Icon.png");
                                    var targetIconForegroundPath = Path.Combine(gameTemplateTargetFolder, "Settings", "Icon Foreground.png");

                                    if ((EditorUtils.DirectoryExists(contentsPath) && 
                                            !EditorUtils.CopyDirectory(contentsPath, targetContentsPath)) ||
                                        !EditorUtils.CopyFile(iconPath, targetIconPath) ||
                                        !EditorUtils.CopyFile(iconPath, targetIconForegroundPath))
                                    {
                                        ShowMessageBox("Failed to copy project resources! Please ensure the target folder exists and you have proper permissions!", "OK", null);

                                        return;
                                    }

                                    try
                                    {
                                        var json = JsonConvert.SerializeObject(appSettings, Formatting.Indented,
                                            Staple.Tooling.Utilities.JsonSettings);

                                        File.WriteAllText(Path.Combine(gameTemplateTargetFolder, "Settings", "AppSettings.json"),
                                            json);
                                    }
                                    catch(System.Exception)
                                    {
                                        ShowMessageBox("Failed to copy project resources! Please ensure the target folder exists and you have proper permissions!", "OK", null);

                                        return;
                                    }

                                    LoadProject(gameTemplateTargetFolder);
                                });
                        });

                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        ImGui.SetCursorPosX(20);

                        ImGui.PushTextWrapPos(ImGui.GetContentRegionAvail().X - 10);

                        ImGui.TextColored(new Color("#F88379"), errorMessage);

                        ImGui.PopTextWrapPos();
                    }
                }
            });

        ImGui.End();
    }
}
