using MessagePack;
using Newtonsoft.Json;
using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;

namespace Baker;

#region Classes
class Vector4Filler
{
    public float x;
    public float y;
    public float z;
    public float w;

    public int index = 0;

    public float this[int index]
    {
        get
        {
            return index switch
            {
                0 => x,
                1 => y,
                2 => z,
                3 => w,
                _ => 0,
            };
        }

        set
        {
            switch(index)
            {
                case 0:

                    x = value;

                    break;

                case 1:

                    y = value;

                    break;

                case 2:

                    z = value;

                    break;

                case 3:

                    w = value;

                    break;
            }
        }
    }

    public void Add(float value)
    {
        switch(index++)
        {
            case 0:

                x = value;

                break;

            case 1:

                y = value;
                
                break;

            case 2:

                z = value;

                break;

            case 3:

                w = value;

                break;

            default:

                break;
        }
    }

    public Vector4Holder ToHolder()
    {
        return new()
        {
            x = x,
            y = y,
            z = z,
            w = w,
        };
    }
}
#endregion

static partial class Program
{
    private static readonly Lock meshMaterialLock = new();

    private static unsafe void ProcessMeshes(AppPlatform platform, string inputPath, string outputPath)
    {
        var meshFiles = new List<string>();

        #region Prepare Tasks
        foreach (var extension in AssetSerialization.MeshExtensions)
        {
            try
            {
                meshFiles.AddRange(Directory.GetFiles(inputPath, $"*.{extension}.meta", SearchOption.AllDirectories));
            }
            catch (Exception)
            {
            }
        }

        Console.WriteLine($"Processing {meshFiles.Count} meshes...");

        RenderWindow.CurrentRenderer = RendererType.Direct3D11;

        var standardShader = ResourceManager.instance.LoadShaderData(AssetSerialization.StandardShaderGUID);

        bool ShaderHasParameter(string name)
        {
            return standardShader?.metadata.uniforms.Any(x => x.name == name) ?? false;
        }

        for (var i = 0; i < meshFiles.Count; i++)
        {
            var meshFileName = meshFiles[i];

            Console.WriteLine($"\t{meshFileName}");

            try
            {
                if (File.Exists(meshFileName) == false)
                {
                    Console.WriteLine($"\t\tError: {meshFileName} doesn't exist");

                    continue;
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"\t\tError: {meshFileName} doesn't exist");

                continue;
            }

            var guid = FindGuid<Mesh>(meshFileName);

            var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(meshFileName));
            var file = Path.GetFileName(meshFileName).Replace(".meta", "");
            var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file);

            {
                var index = outputFile.IndexOf(inputPath);

                if (index >= 0 && index < outputFile.Length)
                {
                    outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
                }
            }

            outputFile = outputFile.Replace("\\", "/").Replace("/./", "/");

            if (ShouldProcessFile(meshFileName, outputFile) == false &&
                ShouldProcessFile(meshFileName.Replace(".meta", ""), outputFile.Replace(".meta", "")) == false)
            {
                continue;
            }

            WorkScheduler.Dispatch(Path.GetFileName(meshFileName.Replace(".meta", "")), () =>
            {
                #endregion
                Console.WriteLine($"\t\t -> {outputFile}");

                string text;
                MeshAssetMetadata metadata;

                try
                {
                    text = File.ReadAllText(meshFileName);
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Failed to read file");

                    return;
                }

                try
                {
                    metadata = JsonConvert.DeserializeObject<MeshAssetMetadata>(text);

                    metadata.guid = guid;
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Metadata is corrupted");

                    return;
                }

                SharpGLTF.Schema2.ModelRoot model = null;

                try
                {
                    model = SharpGLTF.Schema2.ModelRoot.Load(meshFileName.Replace(".meta", ""));

                    if(model == null)
                    {
                        Console.WriteLine($"\t\tError: Failed to import file {meshFileName.Replace(".meta", "")}");

                        return;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to import file: {e}");

                    return;
                }

                if(metadata.frameRate <= 0)
                {
                    metadata.frameRate = 30;
                }

                var meshData = new SerializableMeshAsset
                {
                    metadata = metadata,
                };

                /*
                if (metadata.makeLeftHanded)
                {
                    flags |= Silk.NET.Assimp.PostProcessSteps.MakeLeftHanded;
                }

                if (metadata.flipUVs)
                {
                    flags |= Silk.NET.Assimp.PostProcessSteps.FlipUVs;
                }

                if (metadata.flipWindingOrder)
                {
                    flags |= Silk.NET.Assimp.PostProcessSteps.FlipWindingOrder;
                }

                if (metadata.splitLargeMeshes)
                {
                    flags |= Silk.NET.Assimp.PostProcessSteps.SplitLargeMeshes;
                }

                if (metadata.preTransformVertices)
                {
                    flags |= Silk.NET.Assimp.PostProcessSteps.PreTransformVertices;
                }

                if (metadata.debone)
                {
                    flags |= Silk.NET.Assimp.PostProcessSteps.Debone;
                }

                if (metadata.splitByBoneCount)
                {
                    flags |= Silk.NET.Assimp.PostProcessSteps.SplitByBoneCount;
                }
                */

                #region Materials
                var counter = 0;

                var materialMapping = new List<string>();
                var materialEmbeddedTextures = new Dictionary<string, string>();

                lock(meshMaterialLock)
                {
                    var materials = model.LogicalMaterials;

                    foreach(var material in materials)
                    {
                        var baseName = (material.Name?.Length ?? 0) > 0 ? material.Name : (++counter).ToString();

                        baseName = string.Join('_', baseName.Split(Path.GetInvalidFileNameChars()));

                        var fileName = $"{baseName}.{AssetSerialization.MaterialExtension}";

                        var target = Path.Combine(Path.GetDirectoryName(meshFileName), fileName);
                        var materialGuid = FindGuid<Material>($"{target}.meta");

                        materialMapping.Add(materialGuid);

                        try
                        {
                            if (File.Exists(target))
                            {
                                continue;
                            }
                        }
                        catch (Exception)
                        {
                        }

                        var materialMetadata = new MaterialMetadata()
                        {
                            shader = AssetSerialization.StandardShaderGUID,
                        };

                        if(material.DoubleSided)
                        {
                            materialMetadata.cullingMode = CullingMode.None;
                        }

                        var basePath = Path.GetDirectoryName(meshFileName).Replace(inputPath, "");
                        
                        if(basePath.Length > 0)
                        {
                            basePath = basePath[1..];
                        }

                        foreach(var p in standardShader?.metadata?.instanceParameters ?? [])
                        {
                            switch(p.type)
                            {
                                case ShaderUniformType.Color:

                                    materialMetadata.parameters.Add(p.name, new()
                                    {
                                        type = MaterialParameterType.Color,
                                        source = MaterialParameterSource.Instance,
                                        colorValue = Color.White,
                                    });

                                    break;

                                case ShaderUniformType.Int:

                                    materialMetadata.parameters.Add(p.name, new()
                                    {
                                        type = MaterialParameterType.Int,
                                        source = MaterialParameterSource.Instance,
                                    });

                                    break;

                                case ShaderUniformType.Float:

                                    materialMetadata.parameters.Add(p.name, new()
                                    {
                                        type = MaterialParameterType.Float,
                                        source = MaterialParameterSource.Instance,
                                    });

                                    break;

                                case ShaderUniformType.Vector2:

                                    materialMetadata.parameters.Add(p.name, new()
                                    {
                                        type = MaterialParameterType.Vector2,
                                        source = MaterialParameterSource.Instance,
                                    });

                                    break;

                                case ShaderUniformType.Vector3:

                                    materialMetadata.parameters.Add(p.name, new()
                                    {
                                        type = MaterialParameterType.Vector3,
                                        source = MaterialParameterSource.Instance,
                                    });

                                    break;

                                case ShaderUniformType.Vector4:

                                    materialMetadata.parameters.Add(p.name, new()
                                    {
                                        type = MaterialParameterType.Vector4,
                                        source = MaterialParameterSource.Instance,
                                    });

                                    break;
                            }
                        }

                        void AddColor(string name, bool has, Vector4 color)
                        {
                            if(ShaderHasParameter(name) == false)
                            {
                                return;
                            }

                            var c = Color.White;

                            if (has)
                            {
                                c.r = color.X;
                                c.g = color.Y;
                                c.b = color.Z;
                                c.a = color.W;
                            }

                            materialMetadata.parameters.Add(name, new MaterialParameter()
                            {
                                type = MaterialParameterType.Color,
                                source = MaterialParameterSource.Uniform,
                                colorValue = c,
                            });
                        }

                        var pieces = new Dictionary<string, string>()
                        {
                            { "diffuseColor", "BaseColor" },
                            { "metallicColor", "MetallicRoughness" },
                            { "emissiveColor", "Emissive" },
                        };

                        foreach(var pair in pieces)
                        {
                            var channel = material.FindChannel(pair.Value);

                            try
                            {
                                if (channel.Value.Color is Vector4 c)
                                {
                                    AddColor(pair.Key, true, c);
                                }
                            }
                            catch(Exception)
                            {
                            }
                        }

                        var textures = new Dictionary<string, string>()
                        {
                            { "diffuseTexture", "BaseColor" },
                            { "emissiveTexture", "Emissive" },
                            { "metallicTexture", "MetallicRoughness" },
                            { "occlusionTexture", "OcclusionTexture" },
                            { "normalTexture", "Normal" },
                        };

                        void AddTexture(string name, bool has, SharpGLTF.Schema2.MaterialChannel slot)
                        {
                            if (ShaderHasParameter(name) == false)
                            {
                                return;
                            }

                            var texturePath = "";

                            var mappingU = TextureWrap.Clamp;
                            var mappingV = TextureWrap.Clamp;

                            if (has)
                            {
                                var texture = slot.Texture.PrimaryImage.Content;
                                var path = texture.SourcePath;

                                if(path == null)
                                {
                                    var textureData = texture.Content.ToArray();

                                    var guid = GuidGenerator.Generate().ToString();

                                    var innerFileName = $"{baseName}{slot.Texture.LogicalIndex}";

                                    var extension = texture.FileExtension;

                                    if ((innerFileName?.Length ?? 0) > 0)
                                    {
                                        texturePath = $"{innerFileName}.{extension}";
                                    }
                                    else if (materialEmbeddedTextures.TryGetValue(path, out texturePath) == false)
                                    {
                                        texturePath = $"{guid}.{extension}";

                                        materialEmbeddedTextures.AddOrSetKey(path, texturePath);
                                    }

                                    try
                                    {
                                        var t = Path.Combine(Path.GetDirectoryName(meshFileName), texturePath);

                                        if (File.Exists(t) == false)
                                        {
                                            File.WriteAllBytes(t, textureData);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }

                                    try
                                    {
                                        var t = Path.Combine(Path.GetDirectoryName(meshFileName), $"{texturePath}.meta");

                                        if (File.Exists(t) == false)
                                        {
                                            var metadata = new TextureMetadata()
                                            {
                                                guid = guid,
                                            };

                                            var json = JsonConvert.SerializeObject(metadata, Formatting.Indented,
                                                Staple.Tooling.Utilities.JsonSettings);

                                            File.WriteAllText(t, json);
                                        }
                                        else
                                        {
                                            guid = FindGuid<Texture>(t);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }

                                    texturePath = guid;
                                }
                                else
                                {
                                    var pieces = path.Replace("\\", "/").Split("/").ToList();

                                    texturePath = path;

                                    while (pieces.Count > 0)
                                    {
                                        try
                                        {
                                            var baseP = Path.Combine(Path.GetDirectoryName(meshFileName), string.Join("/", pieces.Take(pieces.Count - 1)));

                                            var directories = Directory.GetDirectories(baseP);

                                            bool Find(string path)
                                            {
                                                var p = Path.Combine(path, pieces.Last()).Replace("\\", "/");

                                                if (File.Exists(p))
                                                {
                                                    texturePath = string.Join("/", pieces);

                                                    if (processedTextures.TryGetValue($"{p}.meta", out var guid))
                                                    {
                                                        texturePath = guid;
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine($"\t\tUnable to find local texture guid for {p}");

                                                        texturePath = "";
                                                    }

                                                    return true;
                                                }

                                                return false;
                                            }

                                            var found = false;

                                            foreach (var directory in directories)
                                            {
                                                found = Find(Path.Combine(baseP, directory));

                                                if (found)
                                                {
                                                    break;
                                                }
                                            }

                                            if (found)
                                            {
                                                break;
                                            }

                                            found = Find(baseP);

                                            if (found)
                                            {
                                                break;
                                            }
                                        }
                                        catch (Exception)
                                        {
                                        }

                                        pieces.RemoveAt(0);
                                    }

                                    if (pieces.Count == 0)
                                    {
                                        Console.WriteLine($"\t\tUnable to find local texture path for {path}");

                                        texturePath = "";
                                    }

                                    //Console.WriteLine($"\t\tSet Texture {name} to {texturePath}");
                                }
                            }

                            if (texturePath.Length > 0)
                            {
                                mappingU = slot.Texture.Sampler.WrapS switch
                                {
                                    SharpGLTF.Schema2.TextureWrapMode.CLAMP_TO_EDGE => TextureWrap.Clamp,
                                    SharpGLTF.Schema2.TextureWrapMode.REPEAT => TextureWrap.Repeat,
                                    SharpGLTF.Schema2.TextureWrapMode.MIRRORED_REPEAT => TextureWrap.Mirror,
                                    _ => TextureWrap.Clamp,
                                };

                                mappingV = slot.Texture.Sampler.WrapT switch
                                {
                                    SharpGLTF.Schema2.TextureWrapMode.CLAMP_TO_EDGE => TextureWrap.Clamp,
                                    SharpGLTF.Schema2.TextureWrapMode.REPEAT => TextureWrap.Repeat,
                                    SharpGLTF.Schema2.TextureWrapMode.MIRRORED_REPEAT => TextureWrap.Mirror,
                                    _ => TextureWrap.Clamp,
                                };
                            }

                            if (ShaderHasParameter($"{name}_UMapping") && ShaderHasParameter($"{name}_VMapping"))
                            {
                                materialMetadata.parameters.Add($"{name}_UMapping", new MaterialParameter()
                                {
                                    type = MaterialParameterType.TextureWrap,
                                    source = MaterialParameterSource.Uniform,
                                    textureWrapValue = mappingU,
                                });

                                materialMetadata.parameters.Add($"{name}_VMapping", new MaterialParameter()
                                {
                                    type = MaterialParameterType.TextureWrap,
                                    source = MaterialParameterSource.Uniform,
                                    textureWrapValue = mappingV,
                                });
                            }

                            materialMetadata.parameters.Add(name, new MaterialParameter()
                            {
                                type = MaterialParameterType.Texture,
                                source = MaterialParameterSource.Uniform,
                                textureValue = texturePath,
                            });
                        }

                        foreach(var pair in textures)
                        {
                            var texture = material.FindChannel(pair.Key);

                            if(texture.HasValue)
                            {
                                AddTexture(pair.Key, true, texture.Value);
                            }
                        }

                        //TODO
                        /*
                        if (material.IsPBRMaterial)
                        {
                            AddTexture("baseColorTexture", material.PBR.HasTextureBaseColor, material.PBR.TextureBaseColor);
                            AddTexture("roughnessTexture", material.PBR.HasTextureRoughness, material.PBR.TextureRoughness);
                            AddTexture("metalnessTexture", material.PBR.HasTextureMetalness, material.PBR.TextureMetalness);
                            AddTexture("normalCameraTexture", material.PBR.HasTextureNormalCamera, material.PBR.TextureNormalCamera);
                            AddTexture("emissionColorTexture", material.PBR.HasTextureEmissionColor, material.PBR.TextureEmissionColor);
                        }
                        */

                        try
                        {
                            var json = JsonConvert.SerializeObject(materialMetadata, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                            File.WriteAllText(target, json);
                        }
                        catch (Exception)
                        {
                        }

                        try
                        {
                            var assetHolder = new AssetHolder()
                            {
                                guid = materialGuid,
                                typeName = typeof(Material).FullName,
                            };

                            var json = JsonConvert.SerializeObject(assetHolder, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                            File.WriteAllText($"{target}.meta", json);
                        }
                        catch (Exception)
                        {
                        }

                        Console.WriteLine($"\t\tGenerated material {target}");
                    }
                }
                #endregion

                Vector3Holder ApplyTransform(Vector3Holder value)
                {
                    return value;
                }

                Vector3Holder ApplyNormalTransform(Vector3Holder value)
                {
                    //Must reverse the angle
                    var rotation = metadata.rotation switch
                    {
                        MeshAssetRotation.NinetyPositive => Matrix4x4.CreateRotationX(Staple.Math.Deg2Rad * -90),
                        MeshAssetRotation.NinetyNegative => Matrix4x4.CreateRotationX(Staple.Math.Deg2Rad * 90),
                        _ => Matrix4x4.Identity,
                    };

                    return new(Vector3.Normalize(Vector3.TransformNormal(value.ToVector3(), rotation)));
                }

                var nodes = new List<MeshAssetNode>();

                var nodeCounters = new Dictionary<string, int>();
                var nodeToName = new Dictionary<SharpGLTF.Schema2.Node, string>();
                var nodeToIndex = new Dictionary<SharpGLTF.Schema2.Node, int>();
                var localNodes = new Dictionary<int, SharpGLTF.Schema2.Node>();

                var meshCounter = 0;

                void RegisterNode(SharpGLTF.Schema2.Node node, MeshAssetNode parent)
                {
                   var nodeName = node.Name;

                    if (nodeCounters.TryGetValue(nodeName, out var counter))
                    {
                        counter++;

                        nodeCounters.AddOrSetKey(nodeName, counter);
                    }
                    else
                    {
                        counter = 0;

                        nodeCounters.Add(nodeName, counter);
                    }

                    nodeName = counter == 0 ? nodeName : $"{nodeName}{counter}";

                    var scale = node.LocalTransform.GetDecomposed().Scale;
                    var rotation = node.LocalTransform.GetDecomposed().Rotation;
                    var translation = node.LocalTransform.GetDecomposed().Translation;

                    var meshIndices = new List<int>();

                    var newNode = new MeshAssetNode()
                    {
                        name = nodeName,
                        meshIndices = meshIndices,
                        position = new Vector3Holder(translation),
                        scale = new Vector3Holder(scale),
                        rotation = new Vector3Holder(rotation),
                    };

                    var currentIndex = nodes.Count;

                    nodeToName.Add(node, nodeName);
                    nodeToIndex.Add(node, currentIndex);
                    localNodes.Add(currentIndex, node);

                    if (parent != null)
                    {
                        parent.children.Add(nodes.Count);
                    }

                    nodes.Add(newNode);

                    #region Mesh
                    if (node.Mesh != null)
                    {
                        var mesh = node.Mesh;

                        foreach (var primitive in mesh.Primitives)
                        {
                            meshIndices.Add(meshCounter++);

                            var m = new MeshAssetMeshInfo
                            {
                                name = $"{mesh.Name} {primitive.LogicalIndex}",
                                materialGuid = primitive.Material.LogicalIndex >= 0 && primitive.Material.LogicalIndex < materialMapping.Count ?
                                    materialMapping[primitive.Material.LogicalIndex] : "",
                                type = node.Skin != null &&
                                    primitive.VertexAccessors.ContainsKey("JOINTS_0") &&
                                    primitive.VertexAccessors.ContainsKey("WEIGHTS_0") ? MeshAssetType.Skinned : MeshAssetType.Normal,
                            };

                            switch (primitive.DrawPrimitiveType)
                            {
                                case SharpGLTF.Schema2.PrimitiveType.TRIANGLES:

                                    m.topology = MeshTopology.Triangles;

                                    break;

                                case SharpGLTF.Schema2.PrimitiveType.TRIANGLE_STRIP:

                                    m.topology = MeshTopology.TriangleStrip;

                                    break;

                                case SharpGLTF.Schema2.PrimitiveType.TRIANGLE_FAN:

                                    //processed later

                                    break;

                                case SharpGLTF.Schema2.PrimitiveType.LINES:

                                    m.topology = MeshTopology.Lines;

                                    break;

                                case SharpGLTF.Schema2.PrimitiveType.LINE_STRIP:

                                    m.topology = MeshTopology.LineStrip;

                                    break;

                                case SharpGLTF.Schema2.PrimitiveType.POINTS:

                                    m.topology = MeshTopology.Points;

                                    break;

                                default:

                                    continue;
                            }

                            var vert = primitive.GetVertexAccessor("POSITION").AsVector3Array();
                            var tan = primitive.GetVertexAccessor("TANGENT")?.AsVector4Array();
                            var nor = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
                            var bi = primitive.GetVertexAccessor("JOINTS_0")?.AsVector4Array();
                            var bw = primitive.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();

                            var texcoords = new IList<Vector2>[]
                            {
                                primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array(),
                                primitive.GetVertexAccessor("TEXCOORD_1")?.AsVector2Array(),
                                primitive.GetVertexAccessor("TEXCOORD_2")?.AsVector2Array(),
                                primitive.GetVertexAccessor("TEXCOORD_3")?.AsVector2Array(),
                                primitive.GetVertexAccessor("TEXCOORD_4")?.AsVector2Array(),
                                primitive.GetVertexAccessor("TEXCOORD_5")?.AsVector2Array(),
                                primitive.GetVertexAccessor("TEXCOORD_6")?.AsVector2Array(),
                                primitive.GetVertexAccessor("TEXCOORD_7")?.AsVector2Array(),
                            };

                            var colors = new IList<Vector4>[]
                            {
                                primitive.GetVertexAccessor("COLOR_0")?.AsVector4Array(),
                                primitive.GetVertexAccessor("COLOR_1")?.AsVector4Array(),
                                primitive.GetVertexAccessor("COLOR_2")?.AsVector4Array(),
                                primitive.GetVertexAccessor("COLOR_3")?.AsVector4Array(),
                            };

                            var vertexCount = vert.Count;

                            var vertices = new List<Vector3Holder>();
                            var tangents = new List<Vector3Holder>();
                            var bitangents = new List<Vector3Holder>();
                            var indices = new List<int>();
                            var normals = nor != null ? new Vector3[vertexCount] : [];

                            var vl = vert.ToArray();

                            var bounds = AABB.CreateFromPoints(vl);

                            m.boundsCenter = new(bounds.center);
                            m.boundsExtents = new(bounds.extents * 2);

                            for (var j = 0; j < vertexCount; j++)
                            {
                                vertices.Add(ApplyTransform(new Vector3Holder(vert[j])));

                                if (tan != null)
                                {
                                    var tangent = tan[j].ToVector3();
                                    tangents.Add(ApplyNormalTransform(new Vector3Holder(tangent)));

                                    if(nor != null)
                                    {
                                        var bitangent = tan[j].W * Vector3.Cross(nor[j], tangent);

                                        bitangents.Add(ApplyNormalTransform(new Vector3Holder(bitangent)));
                                    }
                                }

                                if (nor != null)
                                {
                                    normals[j] = nor[j];
                                }
                            }

                            indices.AddRange(primitive.GetIndices().Select(x => (int)x));

                            void FlipNormals()
                            {
                                for(var i = 0; i < normals.Length; i++)
                                {
                                    normals[i] = -normals[i];
                                }
                            }

                            if (metadata.flipWindingOrder)
                            {
                                switch (primitive.DrawPrimitiveType)
                                {
                                    case SharpGLTF.Schema2.PrimitiveType.TRIANGLES:

                                        for (var k = 0; k < indices.Count; k += 3)
                                        {
                                            (indices[k + 1], indices[k + 2]) = (indices[k + 2], indices[k + 1]);
                                        }

                                        break;

                                    case SharpGLTF.Schema2.PrimitiveType.TRIANGLE_STRIP:

                                        {
                                            var flipped = new List<int>();

                                            for (var i = 0; i < indices.Count - 2; i++)
                                            {
                                                var a = indices[i];
                                                var b = indices[i + 1];
                                                var c = indices[i + 2];

                                                if (i % 2 == 0)
                                                {
                                                    flipped.AddRange([a, c, b]);
                                                }
                                                else
                                                {
                                                    flipped.AddRange([a, b, c]);
                                                }
                                            }

                                            indices = flipped;

                                            m.topology = MeshTopology.Triangles;
                                        }

                                        break;

                                    case SharpGLTF.Schema2.PrimitiveType.TRIANGLE_FAN:

                                        {
                                            var newIndices = new List<int>();

                                            var first = indices[0];

                                            for (var k = 0; k < indices.Count - 1; k++)
                                            {
                                                newIndices.AddRange([first, indices[i + 1], indices[i]]);
                                            }

                                            indices = newIndices;

                                            m.topology = MeshTopology.Triangles;
                                        }

                                        break;
                                }
                            }
                            else if (primitive.DrawPrimitiveType == SharpGLTF.Schema2.PrimitiveType.TRIANGLE_FAN)
                            {
                                var newIndices = new List<int>();

                                var first = indices[0];

                                for (var k = 0; k < indices.Count - 1; k++)
                                {
                                    newIndices.AddRange([first, indices[i], indices[i + 1]]);
                                }

                                indices = newIndices;

                                m.topology = MeshTopology.Triangles;
                            }

                            for (var k = 0; k < 4; k++)
                            {
                                if (colors[k] != null)
                                {
                                    var c = colors[k].Select(x => new Vector4Holder(x));

                                    switch (k)
                                    {
                                        case 0:

                                            m.colors.AddRange(c);

                                            break;

                                        case 1:

                                            m.colors2.AddRange(c);

                                            break;

                                        case 2:

                                            m.colors3.AddRange(c);

                                            break;

                                        case 3:

                                            m.colors4.AddRange(c);

                                            break;
                                    }
                                }
                            }

                            m.vertices = vertices;

                            m.tangents = tangents;

                            m.bitangents = bitangents;

                            m.indices = indices;

                            if (metadata.regenerateNormals)
                            {
                                var v = m.vertices
                                    .Select(x => x.ToVector3())
                                    .ToArray();

                                normals = Mesh.GenerateNormals(v, CollectionsMarshal.AsSpan(m.indices), metadata.useSmoothNormals);
                            }

                            m.normals = normals
                                .Select(x => ApplyNormalTransform(new Vector3Holder(x)))
                                .ToList();

                            var uvs = new List<Vector2Holder>[8]
                            {
                                m.UV1,
                                m.UV2,
                                m.UV3,
                                m.UV4,
                                m.UV5,
                                m.UV6,
                                m.UV7,
                                m.UV8,
                            };

                            for (var j = 0; j < 8; j++)
                            {
                                if (texcoords[j] != null)
                                {
                                    uvs[j].AddRange(texcoords[j].Select(x => new Vector2Holder(x)));
                                }
                            }

                            if (node.Skin != null)
                            {
                                var boneIndices = new List<Vector4Holder>();
                                var boneWeights = new List<Vector4Holder>();

                                var vertexBoneWeights = new Dictionary<int, List<float>>();
                                var boneMapping = new Dictionary<string, int>();

                                var skin = node.Skin;

                                var localBones = new Dictionary<int, int>();

                                for (var j = 0; j < bi.Count; j++)
                                {
                                    var boneIndex = bi[j];
                                    var w = bw[j];

                                    boneIndices.Add(new(boneIndex));
                                    boneWeights.Add(new(w/* / (w.X + w.Y + w.Z + w.W)*/));

                                    for (var k = 0; k < 4; k++)
                                    {
                                        var localBoneIndex = bi[j][k];

                                        if(localBones.ContainsKey((int)localBoneIndex) == false)
                                        {
                                            localBones.Add((int)localBoneIndex, localBones.Count);
                                        }
                                    }
                                }

                                foreach(var pair in localBones)
                                {
                                    foreach(var targetIndices in boneIndices)
                                    {
                                        if(targetIndices.x == pair.Key)
                                        {
                                            targetIndices.x = pair.Value;
                                        }

                                        if (targetIndices.y == pair.Key)
                                        {
                                            targetIndices.y = pair.Value;
                                        }

                                        if (targetIndices.z == pair.Key)
                                        {
                                            targetIndices.z = pair.Value;
                                        }

                                        if (targetIndices.w == pair.Key)
                                        {
                                            targetIndices.w = pair.Value;
                                        }
                                    }
                                }

                                for(var i = 0; i < localBones.Count; i++)
                                {
                                    m.bones.Add(new());
                                }

                                foreach(var pair in localBones)
                                {
                                    var target = pair.Value;

                                    var localBone = skin.Joints[pair.Key];

                                    var offsetMatrix = skin.InverseBindMatrices[pair.Key];

                                    Matrix4x4.Decompose(offsetMatrix, out var localScale, out var localRotation, out var localPosition);

                                    m.bones[target] = new()
                                    {
                                        nodeIndex = localBone.LogicalIndex,
                                        offsetPosition = new(localPosition),
                                        offsetRotation = new(localRotation),
                                        offsetScale = new(localScale),
                                    };
                                }

                                m.boneIndices = boneIndices;

                                m.boneWeights = boneWeights;
                            }

                            meshData.meshes.Add(m);
                        }
                    }
                    #endregion

                    var children = node.VisualChildren;

                    foreach (var child in children)
                    {
                        RegisterNode(child, newNode);
                    }
                }

                foreach(var root in model.DefaultScene.VisualChildren)
                {
                    RegisterNode(root, null);
                }

                meshData.nodes = nodes.ToArray();

                foreach(var mesh in meshData.meshes)
                {
                    foreach(var bone in mesh.bones)
                    {
                        bone.nodeIndex = nodeToIndex[model.LogicalNodes[bone.nodeIndex]];
                    }
                }

                var animations = model.LogicalAnimations;
                var animationCounter = 0;

                for(var j = 0; j < animations.Count; j++)
                {
                    var animation = animations[j];

                    var a = new MeshAssetAnimation()
                    {
                        duration = animation.Duration,
                        ticksPerSecond = metadata.frameRate,
                        name = animation.Name ?? $"Unnamed {++animationCounter}",
                    };

                    var channels = animation.Channels;

                    var nodeChannels = new Dictionary<SharpGLTF.Schema2.Node,
                        (List<MeshAssetVectorAnimationKey>, List<MeshAssetQuaternionAnimationKey>, List<MeshAssetVectorAnimationKey>)>();

                    foreach (var channel in channels)
                    {
                        if (nodeChannels.TryGetValue(channel.TargetNode, out var contents) == false)
                        {
                            contents = new([], [], []);

                            nodeChannels.Add(channel.TargetNode, contents);
                        }

                        var (positionKeys, rotationKeys, scaleKeys) = contents;

                        switch (channel.TargetNodePath)
                        {
                            case SharpGLTF.Schema2.PropertyPath.translation:

                                {
                                    var positionSampler = channel.GetTranslationSampler();
                                    var curveSampler = positionSampler.CreateCurveSampler();

                                    var duration = animation.Duration;
                                    var step = 1.0f / metadata.frameRate;

                                    for (var t = 0.0f; t <= duration; t += step)
                                    {
                                        positionKeys.Add(new()
                                        {
                                            time = t,
                                            value = new(curveSampler.GetPoint(t))
                                        });
                                    }
                                }

                                break;

                            case SharpGLTF.Schema2.PropertyPath.rotation:

                                {
                                    var rotationSampler = channel.GetRotationSampler();
                                    var curveSampler = rotationSampler.CreateCurveSampler();

                                    var duration = animation.Duration;
                                    var step = 1.0f / metadata.frameRate;

                                    for (var t = 0.0f; t <= duration; t += step)
                                    {
                                        rotationKeys.Add(new()
                                        {
                                            time = t,
                                            value = new(curveSampler.GetPoint(t))
                                        });
                                    }
                                }

                                break;

                            case SharpGLTF.Schema2.PropertyPath.scale:

                                {
                                    var scaleSampler = channel.GetScaleSampler();
                                    var curveSampler = scaleSampler.CreateCurveSampler();

                                    var duration = animation.Duration;
                                    var step = 1.0f / metadata.frameRate;

                                    for (var t = 0.0f; t <= duration; t += step)
                                    {
                                        scaleKeys.Add(new()
                                        {
                                            time = t,
                                            value = new(curveSampler.GetPoint(t))
                                        });
                                    }
                                }

                                break;
                        }
                    }

                    foreach(var pair in nodeChannels)
                    {
                        var c = new MeshAssetAnimationChannel()
                        {
                            nodeIndex = nodeToIndex[pair.Key],
                            preState = MeshAssetAnimationStateBehaviour.Default,
                            postState = MeshAssetAnimationStateBehaviour.Default,
                            positionKeys = pair.Value.Item1,
                            rotationKeys = pair.Value.Item2,
                            scaleKeys = pair.Value.Item3,
                        };

                        a.channels.Add(c);
                    }

                    meshData.animations.Add(a);
                }

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
                }
                catch (Exception)
                {
                }

                try
                {
                    Directory.CreateDirectory(outputPath);
                }
                catch (Exception)
                {
                }

                try
                {
                    File.Delete(outputFile);
                }
                catch (Exception)
                {
                }

                try
                {
                    var header = new SerializableMeshAssetHeader();

                    using var stream = File.OpenWrite(outputFile);
                    using var writer = new BinaryWriter(stream);

                    var encoded = MessagePackSerializer.Serialize(header)
                        .Concat(MessagePackSerializer.Serialize(meshData));

                    writer.Write(encoded.ToArray());
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to save mesh asset: {e}");
                }
            });
        }
    }
}
