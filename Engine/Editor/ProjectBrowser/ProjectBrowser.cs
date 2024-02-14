using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Staple.Editor
{
    internal class ProjectBrowser
    {
        public static Dictionary<string, ProjectBrowserResourceType> resourceTypes = new()
        {
            { ".asset", ProjectBrowserResourceType.Asset },
            { ".mat", ProjectBrowserResourceType.Material },
            { ".stsh", ProjectBrowserResourceType.Shader },
            { ".stsc", ProjectBrowserResourceType.Scene },
        };

        static ProjectBrowser()
        {
            void AddAll(string[] extensions, ProjectBrowserResourceType type)
            {
                foreach (var ext in extensions)
                {
                    resourceTypes.Add($".{ext}", type);
                }
            }

            AddAll(AssetSerialization.TextureExtensions, ProjectBrowserResourceType.Texture);
            AddAll(AssetSerialization.AudioExtensions, ProjectBrowserResourceType.Audio);
            AddAll(AssetSerialization.MeshExtensions, ProjectBrowserResourceType.Mesh);
        }

        public const float contentPanelThumbnailSize = 64;

        public const float contentPanelPadding = 16;

        public string basePath;

        public static ProjectBrowserDropType dropType = ProjectBrowserDropType.None;

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
            if(resourceTypes.TryGetValue(extension, out var type))
            {
                return type;
            }

            return ProjectBrowserResourceType.Other;
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

                            case ProjectBrowserResourceType.Mesh:

                                node.typeName = typeof(Mesh).FullName;

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
                            if ((texture?.Disposed ?? true) || ThumbnailCache.HasCachedThumbnail(node.path))
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
                        if(resourceTypes.TryGetValue(node.extension, out var type))
                        {
                            switch(type)
                            {
                                case ProjectBrowserResourceType.Texture:

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

                                case ProjectBrowserResourceType.Material:

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

                                case ProjectBrowserResourceType.Shader:

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

                                case ProjectBrowserResourceType.Scene:

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

                                case ProjectBrowserResourceType.Mesh:

                                    try
                                    {
                                        if (File.Exists($"{node.path}.meta") == false)
                                        {
                                            var jsonData = JsonConvert.SerializeObject(new MeshAssetMetadata()
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

                                case ProjectBrowserResourceType.Audio:

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

                                case ProjectBrowserResourceType.Asset:

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
                            }
                        }
                        else
                        {
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
                "ASSET",
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
                },
                (index, _) =>
                {
                    if(Scene.current == null)
                    {
                        return;
                    }

                    ProjectBrowserNode item = null;

                    if (currentContentNode == null)
                    {
                        currentContentNode = projectBrowserNodes[index];

                        item = currentContentNode;
                    }
                    else
                    {
                        item = currentContentNode.type == ProjectBrowserNodeType.Folder ? (index >= 0 && index < currentContentNode.subnodes.Count ? currentContentNode.subnodes[index] : null) :
                            currentContentNode;
                    }

                    if (item == null)
                    {
                        return;
                    }

                    var cachePath = item.path;

                    var cacheIndex = cachePath.IndexOf("Assets");

                    cachePath = cachePath.Substring(cacheIndex + "Assets0".Length).Replace("\\", "/");

                    var guid = AssetDatabase.GetAssetGuid(cachePath);

                    if(guid == null)
                    {
                        return;
                    }

                    switch(dropType)
                    {
                        case ProjectBrowserDropType.Asset:

                            if (item.typeName == typeof(Mesh).FullName)
                            {
                                var asset = ResourceManager.instance.LoadMeshAsset(guid);

                                var targetEntity = StapleEditor.instance.dropTargetEntity;

                                Transform parent = null;

                                if (targetEntity != Entity.Empty)
                                {
                                    parent = Scene.current.world.GetComponent<Transform>(targetEntity);
                                }

                                var baseEntity = Scene.current.world.CreateEntity();

                                Scene.current.world.SetEntityName(baseEntity, item.name);

                                var baseTransform = Scene.current.world.AddComponent<Transform>(baseEntity);

                                baseTransform.entity = baseEntity;
                                baseTransform.SetParent(parent);

                                var meshIndex = 0;

                                foreach (var mesh in asset.meshes)
                                {
                                    var meshEntity = Scene.current.world.CreateEntity();

                                    Scene.current.world.SetEntityName(meshEntity, mesh.name);

                                    var meshTransform = Scene.current.world.AddComponent<Transform>(meshEntity);

                                    meshTransform.entity = meshEntity;
                                    meshTransform.SetParent(baseTransform);

                                    var meshRenderer = Scene.current.world.AddComponent<MeshRenderer>(meshEntity);

                                    var targetPath = StapleEditor.instance.ProjectNodeCachePath($"{Path.GetDirectoryName(item.path)}/{Path.GetFileNameWithoutExtension(item.path)} {mesh.materialIndex + 1}.mat");

                                    meshRenderer.material = ResourceManager.instance.LoadMaterial(targetPath);

                                    meshRenderer.mesh = ResourceManager.instance.LoadMesh($"{guid}:{meshIndex++}");
                                }
                            }

                            break;

                        case ProjectBrowserDropType.SceneList:

                            if(BuildWindow.instance.TryGetTarget(out var buildWindow))
                            {
                                buildWindow.AddScene(guid);
                            }

                            break;
                    }

                    dropType = ProjectBrowserDropType.None;
                });

            var result = ImGui.IsWindowHovered();

            ImGui.EndChild();

            ImGui.Columns();

            return result;
        }
    }
}
