using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Xml.Linq;

namespace Staple.Editor
{
    internal class ProjectBrowser
    {
        public const float contentPanelThumbnailSize = 64;

        public const float contentPanelPadding = 16;

        public string basePath;

        internal List<ProjectBrowserNode> projectBrowserNodes = new();

        public ProjectBrowserNode currentContentNode;

        private List<ImGuiUtils.ContentGridItem> currentContentBrowserNodes = new();

        internal Dictionary<string, Texture> editorResources = new();

        public void LoadEditorTexture(string name, string path)
        {
            path = Path.Combine(Environment.CurrentDirectory, "Editor Resources", path);

            try
            {
                var texture = Texture.CreateStandard(path, File.ReadAllBytes(path), StandardTextureColorComponents.RGBA);

                if (texture != null)
                {
                    editorResources.Add(name, texture);
                }
            }
            catch (Exception)
            {
            }
        }

        public void UpdateProjectBrowserNodes()
        {
            if (basePath == null)
            {
                projectBrowserNodes = new List<ProjectBrowserNode>();
            }
            else
            {
                projectBrowserNodes = new List<ProjectBrowserNode>();

                void Recursive(string p, List<ProjectBrowserNode> nodes)
                {
                    string[] directories = Array.Empty<string>();
                    string[] files = Array.Empty<string>();

                    try
                    {
                        directories = Directory.GetDirectories(p);
                    }
                    catch (Exception)
                    {
                    }

                    try
                    {
                        files = Directory.GetFiles(p);
                    }
                    catch (Exception)
                    {
                    }

                    foreach (var directory in directories)
                    {
                        var subnodes = new List<ProjectBrowserNode>();

                        Recursive(directory, subnodes);

                        nodes.Add(new ProjectBrowserNode()
                        {
                            name = Path.GetFileName(directory),
                            extension = "",
                            path = directory,
                            type = ProjectBrowserNodeType.Folder,
                            subnodes = subnodes,
                        });
                    }

                    foreach (var file in files)
                    {
                        if (file.EndsWith(".meta"))
                        {
                            continue;
                        }

                        var node = new ProjectBrowserNode()
                        {
                            name = Path.GetFileNameWithoutExtension(file),
                            extension = Path.GetExtension(file),
                            path = file,
                            subnodes = new(),
                            type = ProjectBrowserNodeType.File
                        };

                        nodes.Add(node);

                        switch (node.extension)
                        {
                            case ".mat":

                                node.resourceType = ProjectResourceType.Material;

                                break;

                            case ".stsh":

                                node.resourceType = ProjectResourceType.Shader;

                                break;

                            case ".stsc":

                                node.resourceType = ProjectResourceType.Scene;
                                node.action = ProjectBrowserNodeAction.InspectScene;

                                break;

                            case ".png":
                            case ".jpg":
                            case ".jpeg":
                            case ".gif":
                            case ".bmp":

                                node.resourceType = ProjectResourceType.Texture;

                                break;

                            default:

                                node.resourceType = ProjectResourceType.Other;

                                break;
                        }
                    }
                }

                Recursive(Path.Combine(basePath, "Assets"), projectBrowserNodes);

                UpdateCurrentContentNodes(projectBrowserNodes);
            }
        }

        public void UpdateCurrentContentNodes(List<ProjectBrowserNode> nodes)
        {
            currentContentBrowserNodes.Clear();

            foreach (var node in nodes)
            {
                if (node.path.EndsWith(".meta"))
                {
                    continue;
                }

                var item = new ImGuiUtils.ContentGridItem()
                {
                    name = node.name,
                };

                switch (node.type)
                {
                    case ProjectBrowserNodeType.File:

                        switch (node.resourceType)
                        {
                            case ProjectResourceType.Texture:

                                item.ensureValidTexture = (texture) =>
                                {
                                    if (texture?.Disposed ?? true)
                                    {
                                        return ThumbnailCache.GetThumbnail(node.path);
                                    }

                                    return texture;
                                };

                                break;

                            default:

                                item.ensureValidTexture = (texture) =>
                                {
                                    if (texture?.Disposed ?? true)
                                    {
                                        if (editorResources.TryGetValue("FileIcon", out texture))
                                        {
                                            return texture;
                                        }
                                    }

                                    return texture;
                                };

                                break;
                        }

                        break;

                    case ProjectBrowserNodeType.Folder:

                        item.ensureValidTexture = (texture) =>
                        {
                            if (texture?.Disposed ?? true)
                            {
                                if (editorResources.TryGetValue("FolderIcon", out texture))
                                {
                                    return texture;
                                }
                            }

                            return texture;
                        };

                        break;
                }

                currentContentBrowserNodes.Add(item);
            }
        }

        public void CreateMissingMetaFiles()
        {
            void Recursive(List<ProjectBrowserNode> nodes)
            {
                foreach (var node in nodes)
                {
                    static string Hash()
                    {
                        //Guid collision fix
                        Thread.Sleep(25);

                        return Guid.NewGuid().ToString();
                    }

                    if (node.type == ProjectBrowserNodeType.Folder)
                    {
                        {
                            try
                            {
                                if (File.Exists($"{node.path}.meta") == false)
                                {
                                    File.WriteAllText($"{node.path}.meta", Hash());
                                }
                            }
                            catch (System.Exception)
                            {
                            }
                        }

                        Recursive(node.subnodes);
                    }
                    else
                    {
                        switch (node.resourceType)
                        {
                            case ProjectResourceType.Texture:
                                {
                                    try
                                    {
                                        if (File.Exists($"{node.path}.meta") == false)
                                        {
                                            var jsonData = JsonConvert.SerializeObject(new TextureMetadata()
                                            {
                                                guid = Hash(),
                                            },
                                            Formatting.Indented, new JsonSerializerSettings()
                                            {
                                                Converters =
                                                {
                                                    new StringEnumConverter(),
                                                }
                                            });

                                            File.WriteAllText($"{node.path}.meta", jsonData);
                                        }
                                    }
                                    catch (System.Exception)
                                    {
                                    }
                                }

                                break;

                            default:

                                if (node.path.EndsWith(".meta") == false)
                                {
                                    try
                                    {
                                        if (File.Exists($"{node.path}.meta") == false)
                                        {
                                            File.WriteAllText($"{node.path}.meta", Hash());
                                        }
                                    }
                                    catch (System.Exception)
                                    {
                                    }
                                }

                                break;
                        }
                    }
                }
            }

            Recursive(projectBrowserNodes);
        }

        public void Draw(ImGuiIOPtr io, Action<ProjectBrowserNode> onClick, Action<ProjectBrowserNode> onDoubleClick)
        {
            ImGui.Columns(2, "ProjectBrowserContent");

            var nextNode = currentContentNode;

            void Recursive(ProjectBrowserNode node)
            {
                if (node.type != ProjectBrowserNodeType.Folder)
                {
                    return;
                }

                var flags = ImGuiTreeNodeFlags.SpanFullWidth;
                var hasChildren = node.subnodes.Any(x => x.type == ProjectBrowserNodeType.Folder);

                if (hasChildren == false)
                {
                    flags |= ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.Leaf;
                }

                if (ImGui.TreeNodeEx($"{node.name}##0", flags))
                {
                    if (ImGui.IsItemClicked())
                    {
                        nextNode = node;
                    }

                    if (hasChildren)
                    {
                        foreach (var subnode in node.subnodes)
                        {
                            Recursive(subnode);
                        }

                        ImGui.TreePop();
                    }
                }
            }

            if (ImGui.TreeNodeEx("Assets", ImGuiTreeNodeFlags.SpanFullWidth))
            {
                if (ImGui.IsItemClicked())
                {
                    currentContentNode = null;

                    UpdateCurrentContentNodes(projectBrowserNodes);
                }

                foreach (var node in projectBrowserNodes)
                {
                    Recursive(node);
                }

                ImGui.TreePop();
            }

            ImGui.NextColumn();

            if(nextNode != null && currentContentNode != nextNode)
            {
                currentContentNode = nextNode;

                UpdateCurrentContentNodes(nextNode.subnodes);
            }

            ImGui.BeginChild("ProjectBrowserContentAssets");

            ImGuiUtils.ContentGrid(currentContentBrowserNodes, contentPanelPadding, contentPanelThumbnailSize,
                (index, _) =>
                {
                    ProjectBrowserNode item = null;

                    if (currentContentNode == null)
                    {
                        item = projectBrowserNodes[index];
                    }
                    else
                    {
                        item = index >= 0 && index < currentContentNode.subnodes.Count ? currentContentNode.subnodes[index] : null;
                    }

                    if (item == null)
                    {
                        return;
                    }

                    onClick?.Invoke(item);
                },
                (index, _) =>
                {
                    ProjectBrowserNode item = null;

                    if (currentContentNode == null)
                    {
                        currentContentNode = projectBrowserNodes[index];

                        item = currentContentNode;
                    }
                    else
                    {
                        item = index >= 0 && index < currentContentNode.subnodes.Count ? currentContentNode.subnodes[index] : null;
                    }

                    if (item == null)
                    {
                        return;
                    }

                    if (item.subnodes.Count == 0)
                    {
                        if (item.type == ProjectBrowserNodeType.File)
                        {
                            onDoubleClick?.Invoke(item);
                        }
                    }
                    else
                    {
                        currentContentNode = item;

                        UpdateCurrentContentNodes(item.subnodes);
                    }
                });

            ImGui.EndChild();

            ImGui.Columns();
        }
    }
}
