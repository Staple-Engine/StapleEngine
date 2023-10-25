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

        private readonly List<ImGuiUtils.ContentGridItem> currentContentBrowserNodes = new();

        internal Dictionary<string, Texture> editorResources = new();

        public Texture GetEditorResource(string name)
        {
            return editorResources.TryGetValue(name, out var texture) ? texture : null;
        }

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

        internal static ProjectBrowserResourceType ResourceTypeForExtension(string extension)
        {
            switch (extension)
            {
                case ".asset":

                    return ProjectBrowserResourceType.Asset;

                case ".mat":

                    return ProjectBrowserResourceType.Material;

                case ".stsh":

                    return ProjectBrowserResourceType.Shader;

                case ".stsc":

                    return ProjectBrowserResourceType.Scene;

                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".gif":
                case ".bmp":

                    return ProjectBrowserResourceType.Texture;

                case ".ogg":

                    return ProjectBrowserResourceType.Audio;

                default:

                    return ProjectBrowserResourceType.Other;
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

                static void Recursive(string p, List<ProjectBrowserNode> nodes)
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
                            typeName = typeof(FolderAsset).FullName,
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

                        switch (ResourceTypeForExtension(node.extension))
                        {
                            case ProjectBrowserResourceType.Asset:

                                try
                                {
                                    var text = File.ReadAllText($"{file}.meta");
                                    var holder = JsonConvert.DeserializeObject<AssetHolder>(text);

                                    if(holder != null)
                                    {
                                        node.typeName = holder.typeName;
                                    }
                                }
                                catch(Exception)
                                {
                                    node.typeName = "";
                                }

                                break;

                            case ProjectBrowserResourceType.Material:

                                node.typeName = typeof(Material).FullName;

                                break;

                            case ProjectBrowserResourceType.Shader:

                                node.typeName = typeof(Shader).FullName;

                                break;

                            case ProjectBrowserResourceType.Scene:

                                node.action = ProjectBrowserNodeAction.InspectScene;
                                node.typeName = typeof(Scene).FullName;

                                break;

                            case ProjectBrowserResourceType.Texture:

                                node.typeName = typeof(Texture).FullName;

                                break;

                            case ProjectBrowserResourceType.Audio:

                                node.typeName = typeof(AudioClip).FullName;

                                break;

                            default:

                                node.typeName = "Unknown";

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

            if(nodes != projectBrowserNodes)
            {
                ThumbnailCache.Clear();
            }

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

                        item.ensureValidTexture = (texture) =>
                        {
                            if (texture?.Disposed ?? true)
                            {
                                return ThumbnailCache.GetThumbnail(node.path) ?? GetEditorResource("FileIcon");
                            }

                            return texture;
                        };

                        break;

                    case ProjectBrowserNodeType.Folder:

                        item.ensureValidTexture = (texture) =>
                        {
                            if (texture?.Disposed ?? true)
                            {
                                return GetEditorResource("FolderIcon");
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
            static void Recursive(List<ProjectBrowserNode> nodes)
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
                        try
                        {
                            if (File.Exists($"{node.path}.meta") == false)
                            {
                                var holder = new FolderAsset()
                                {
                                    guid = Hash(),
                                    typeName = typeof(FolderAsset).FullName,
                                };

                                var json = JsonConvert.SerializeObject(holder, Formatting.Indented);

                                File.WriteAllText($"{node.path}.meta", json);
                            }
                        }
                        catch (Exception)
                        {
                        }

                        Recursive(node.subnodes);
                    }
                    else
                    {
                        switch(node.extension)
                        {
                            case ".png":
                            case ".jpg":
                            case ".jpeg":
                            case ".gif":
                            case ".bmp":

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
                                catch (Exception)
                                {
                                }

                                break;

                            case ".mat":

                                try
                                {
                                    if (File.Exists($"{node.path}.meta") == false)
                                    {
                                        var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                        {
                                            guid = Hash(),
                                            typeName = typeof(Material).FullName,
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
                                catch (Exception)
                                {
                                }

                                break;

                            case ".stsh":

                                try
                                {
                                    if (File.Exists($"{node.path}.meta") == false)
                                    {
                                        var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                        {
                                            guid = Hash(),
                                            typeName = typeof(Shader).FullName,
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
                                catch (Exception)
                                {
                                }

                                break;

                            case ".stsc":

                                try
                                {
                                    if (File.Exists($"{node.path}.meta") == false)
                                    {
                                        var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                        {
                                            guid = Hash(),
                                            typeName = typeof(Scene).FullName,
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
                                catch (Exception)
                                {
                                }

                                break;

                            case ".ogg":

                                try
                                {
                                    if (File.Exists($"{node.path}.meta") == false)
                                    {
                                        var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                        {
                                            guid = Hash(),
                                            typeName = typeof(AudioClip).FullName,
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
                                catch (Exception)
                                {
                                }

                                break;

                            case ".asset":

                                try
                                {
                                    if (File.Exists($"{node.path}.meta") == false)
                                    {
                                        var text = File.ReadAllText(node.path);
                                        var holder = JsonConvert.DeserializeObject<AssetHolder>(text);

                                        if (holder != null)
                                        {
                                            var json = JsonConvert.SerializeObject(holder, Formatting.Indented);

                                            File.WriteAllText($"{node.path}.meta", json);
                                        }
                                        else
                                        {
                                            var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                            {
                                                guid = Hash(),
                                                typeName = "Unknown",
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
                                }
                                catch (Exception)
                                {
                                }

                                break;

                            default:

                                if (node.path.EndsWith(".meta") == false)
                                {
                                    try
                                    {
                                        if (File.Exists($"{node.path}.meta") == false)
                                        {
                                            var holder = new AssetHolder()
                                            {
                                                guid = Hash(),
                                                typeName = "Unknown",
                                            };

                                            var json = JsonConvert.SerializeObject(holder, Formatting.Indented);

                                            File.WriteAllText($"{node.path}.meta", json);
                                        }
                                    }
                                    catch (Exception)
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

        public bool Draw(ImGuiIOPtr io, Action<ProjectBrowserNode> onClick, Action<ProjectBrowserNode> onDoubleClick)
        {
            ImGui.Columns(2, "ProjectBrowserContent");

            var nextNode = currentContentNode;

            void Recursive(ProjectBrowserNode node)
            {
                if (node.type != ProjectBrowserNodeType.Folder)
                {
                    return;
                }

                var flags = ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.OpenOnArrow;
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

            if (ImGui.TreeNodeEx("Assets", ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnArrow))
            {
                if (ImGui.IsItemClicked())
                {
                    currentContentNode = nextNode = null;

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

            var result = ImGui.IsWindowHovered();

            ImGui.EndChild();

            ImGui.Columns();

            return result;
        }
    }
}
