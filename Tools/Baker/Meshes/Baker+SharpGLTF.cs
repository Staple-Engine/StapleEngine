using Newtonsoft.Json;
using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Baker;

public partial class Program
{
    public static SerializableMeshAsset ProcessSharpGLTFMesh(MeshAssetMetadata metadata, string meshFileName, string inputPath,
        SerializableShader standardShader, Func<string, bool> ShaderHasParameter)
    {
        SharpGLTF.Schema2.ModelRoot model = null;

        var textureData = new RawTextureData()
        {
            colorComponents = StandardTextureColorComponents.RGB,
            data = new byte[256 * 256 * 3],
            width = 256,
            height = 256,
        };

        var placeholder = new ArraySegment<byte>(textureData.EncodePNG());

        var directoryName = Path.GetDirectoryName(meshFileName);

        ArraySegment<byte> FileReader(string assetName)
        {
            assetName = Uri.UnescapeDataString(assetName);

            var filePath = Path.Combine(directoryName, assetName);

            if (File.Exists(filePath))
            {
                return File.ReadAllBytes(filePath);
            }

            return placeholder;
        }

        try
        {
            var context = SharpGLTF.Schema2.ReadContext.Create(FileReader);

            //Not skipping causes models with issues such as material values outside of the allowed range to not load at all,
            //which is not acceptable.
            context.Validation = SharpGLTF.Validation.ValidationMode.Skip;

            model = SharpGLTF.Schema2.ModelRoot.Load(Path.GetFileName(meshFileName), context);

            if (model == null)
            {
                Console.WriteLine($"\t\tError: Failed to import file {meshFileName}");

                return null;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"\t\tError: Failed to import file {meshFileName}: {e}");

            return null;
        }

        if (metadata.frameRate <= 0)
        {
            metadata.frameRate = 30;
        }

        var meshData = new SerializableMeshAsset
        {
            metadata = metadata,
        };

        #region Materials
        var counter = 0;

        var materialMapping = new List<string>();
        var materialEmbeddedTextures = new Dictionary<string, string>();

        lock (meshMaterialLock)
        {
            var materials = model.LogicalMaterials;

            foreach (var material in materials)
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

                if (material.DoubleSided)
                {
                    materialMetadata.cullingMode = CullingMode.None;
                }

                var basePath = Path.GetDirectoryName(meshFileName).Replace(inputPath, "");

                if (basePath.Length > 0)
                {
                    basePath = basePath[1..];
                }

                foreach (var p in standardShader?.metadata?.instanceParameters ?? [])
                {
                    switch (p.type)
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
                    if (ShaderHasParameter(name) == false)
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

                foreach (var pair in pieces)
                {
                    var channel = material.FindChannel(pair.Value);

                    try
                    {
                        if (channel.Value.Color is Vector4 c)
                        {
                            AddColor(pair.Key, true, c);
                        }
                    }
                    catch (Exception)
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

                        if (path == null)
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
                            if (Path.IsPathRooted(path) == false)
                            {
                                var localPath = Path.GetDirectoryName(meshFileName);

                                path = Path.GetFullPath(path, Path.GetDirectoryName(meshFileName));

                                var index = path.IndexOf(localPath);

                                if (index >= 0)
                                {
                                    path = path.Substring(index + localPath.Length + 1);
                                }

                                path = path.Replace(Path.PathSeparator, '/');
                            }

                            var pieces = path.Replace("\\", "/").Split("/").ToList();

                            texturePath = path;

                            var initialPath = Path.Combine(Path.GetDirectoryName(meshFileName), path);

                            var ok = false;

                            if (File.Exists(initialPath))
                            {
                                if (processedTextures.TryGetValue($"{initialPath}.meta", out var guid))
                                {
                                    texturePath = guid;

                                    ok = true;
                                }
                            }

                            if (ok == false)
                            {
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

                foreach (var pair in textures)
                {
                    var texture = material.FindChannel(pair.Key);

                    if (texture.HasValue)
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
            /*
            //Must reverse the angle
            var rotation = metadata.rotation switch
            {
                MeshAssetRotation.NinetyPositive => Matrix4x4.CreateRotationX(Staple.Math.Deg2Rad * -90),
                MeshAssetRotation.NinetyNegative => Matrix4x4.CreateRotationX(Staple.Math.Deg2Rad * 90),
                _ => Matrix4x4.Identity,
            };

            return new(Vector3.Normalize(Vector3.TransformNormal(value.ToVector3(), rotation)));
            */

            return value;
        }

        var nodes = new List<MeshAssetNode>();

        var nodeToName = new Dictionary<SharpGLTF.Schema2.Node, string>();
        var nodeToIndex = new Dictionary<SharpGLTF.Schema2.Node, int>();
        var localNodes = new Dictionary<int, SharpGLTF.Schema2.Node>();

        var meshCounter = 0;

        void RegisterNode(SharpGLTF.Schema2.Node node, MeshAssetNode parent)
        {
            var nodeName = node.Name;

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

            parent?.children.Add(nodes.Count);

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
                        materialGuid = (primitive.Material?.LogicalIndex ?? -1) >= 0 && primitive.Material.LogicalIndex < materialMapping.Count ?
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

                    List<Vector2> SafeGetVertexAccessorVector2(string name, bool debug)
                    {
                        var accessor = primitive.GetVertexAccessor(name);

                        if((accessor?.Count ?? 0) == 0)
                        {
                            return null;
                        }

                        if(accessor.Dimensions == SharpGLTF.Schema2.DimensionType.VEC2)
                        {
                            return [.. accessor.AsVector2Array()];
                        }

                        if (debug)
                        {
                            Log.Debug($"Failed to get Vector2 vertex accessor {name}: Dimensions were {accessor.Dimensions}");
                        }

                        return null;
                    }

                    List<Vector3> SafeGetVertexAccessorVector3(string name, bool debug)
                    {
                        var accessor = primitive.GetVertexAccessor(name);

                        if ((accessor?.Count ?? 0) == 0)
                        {
                            return null;
                        }

                        if (accessor.Dimensions == SharpGLTF.Schema2.DimensionType.VEC3)
                        {
                            return [.. accessor.AsVector3Array()];
                        }

                        if(debug)
                        {
                            Log.Debug($"Failed to get Vector3 vertex accessor {name}: Dimensions were {accessor.Dimensions}");
                        }

                        return null;
                    }

                    List<Vector4> SafeGetVertexAccessorVector4(string name, bool debug)
                    {
                        var accessor = primitive.GetVertexAccessor(name);

                        if ((accessor?.Count ?? 0) == 0)
                        {
                            return null;
                        }

                        if (accessor.Dimensions == SharpGLTF.Schema2.DimensionType.VEC4)
                        {
                            return [.. accessor.AsVector4Array()];
                        }

                        if (debug)
                        {
                            Log.Debug($"Failed to get Vector4 vertex accessor {name}: Dimensions were {accessor.Dimensions}");
                        }

                        return null;
                    }

                    List<Vector4> SafeGetVertexColor(string name)
                    {
                        var l = SafeGetVertexAccessorVector4(name, false);

                        if(l != null)
                        {
                            return l;
                        }

                        var l2 = SafeGetVertexAccessorVector3(name, false);

                        if(l2 != null)
                        {
                            var newList = new Vector4[l2.Count];

                            for(var i = 0; i < l2.Count; i++)
                            {
                                newList[i] = new(l2[i], 1);
                            }

                            return [.. newList];
                        }

                        return null;
                    }

                    var vert = SafeGetVertexAccessorVector3("POSITION", true);
                    var tan = SafeGetVertexAccessorVector4("TANGENT", true);
                    var nor = SafeGetVertexAccessorVector3("NORMAL", true);
                    var bi = SafeGetVertexAccessorVector4("JOINTS_0", true);
                    var bw = SafeGetVertexAccessorVector4("WEIGHTS_0", true);

                    var texcoords = new IList<Vector2>[]
                    {
                        SafeGetVertexAccessorVector2("TEXCOORD_0", true),
                        SafeGetVertexAccessorVector2("TEXCOORD_1", true),
                        SafeGetVertexAccessorVector2("TEXCOORD_2", true),
                        SafeGetVertexAccessorVector2("TEXCOORD_3", true),
                        SafeGetVertexAccessorVector2("TEXCOORD_4", true),
                        SafeGetVertexAccessorVector2("TEXCOORD_5", true),
                        SafeGetVertexAccessorVector2("TEXCOORD_6", true),
                        SafeGetVertexAccessorVector2("TEXCOORD_7", true),
                    };

                    var colors = new IList<Vector4>[]
                    {
                        SafeGetVertexColor("COLOR_0"),
                        SafeGetVertexColor("COLOR_1"),
                        SafeGetVertexColor("COLOR_2"),
                        SafeGetVertexColor("COLOR_3"),
                    };

                    var vertexCount = vert.Count;

                    var vertices = new List<Vector3Holder>();
                    var tangents = new List<Vector3Holder>();
                    var bitangents = new List<Vector3Holder>();
                    var indices = new List<int>();
                    var normals = new Vector3[vertexCount];

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

                            if (nor != null)
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

                                    for (var k = 0; k < indices.Count - 2; k++)
                                    {
                                        var a = indices[k];
                                        var b = indices[k + 1];
                                        var c = indices[k + 2];

                                        if (k % 2 == 0)
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

                                    for (var k = 1; k < indices.Count - 1; k++)
                                    {
                                        newIndices.AddRange([first, indices[k + 1], indices[k]]);
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

                        for (var k = 1; k < indices.Count - 1; k++)
                        {
                            newIndices.AddRange([first, indices[k], indices[k + 1]]);
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

                        var skin = node.Skin;

                        var localBones = new Dictionary<int, int>();

                        for (var j = 0; j < bi.Count; j++)
                        {
                            var boneIndex = bi[j];
                            var w = bw[j];

                            for (var k = 0; k < 4; k++)
                            {
                                var localBoneIndex = bi[j][k];

                                if (localBones.TryGetValue((int)localBoneIndex, out var newIndex) == false)
                                {
                                    newIndex = localBones.Count;

                                    localBones.Add((int)localBoneIndex, newIndex);
                                }

                                boneIndex[k] = newIndex;
                            }

                            boneIndices.Add(new(boneIndex));
                            boneWeights.Add(new(w / (w.X + w.Y + w.Z + w.W)));
                        }

                        for (var j = 0; j < localBones.Count; j++)
                        {
                            m.bones.Add(new());
                        }

                        foreach (var pair in localBones)
                        {
                            var target = pair.Value;

                            var localBone = skin.Joints[pair.Key];

                            var offsetMatrix = skin.InverseBindMatrices[pair.Key];

                            m.bones[target] = new()
                            {
                                nodeIndex = localBone.LogicalIndex,
                                offsetMatrix = Matrix4x4Holder.FromMatrix(offsetMatrix),
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

        MeshAssetNode rootNode = null;

        if (metadata.scale != 1 || metadata.rotation != MeshAssetRotation.None)
        {
            var rotation = metadata.rotation switch
            {
                MeshAssetRotation.NinetyPositive => Quaternion.CreateFromAxisAngle(new(1, 0, 0), 90 * Staple.Math.Deg2Rad),
                MeshAssetRotation.NinetyNegative => Quaternion.CreateFromAxisAngle(new(1, 0, 0), -90 * Staple.Math.Deg2Rad),
                _ => Quaternion.Identity,
            };

            var scale = Vector3.One * metadata.scale;

            rootNode = new()
            {
                name = "StapleRoot",
                position = new(),
                rotation = new(rotation),
                scale = new(scale),
            };

            nodes.Add(rootNode);
        }

        foreach (var root in model.DefaultScene.VisualChildren)
        {
            RegisterNode(root, null);
        }

        meshData.nodes = nodes.ToArray();

        foreach (var mesh in meshData.meshes)
        {
            foreach (var bone in mesh.bones)
            {
                bone.nodeIndex = nodeToIndex[model.LogicalNodes[bone.nodeIndex]];
            }
        }

        var animations = model.LogicalAnimations;
        var animationCounter = 0;

        for (var j = 0; j < animations.Count; j++)
        {
            var animation = animations[j];

            var a = new MeshAssetAnimation()
            {
                duration = animation.Duration,
                name = animation.Name ?? $"Unnamed {++animationCounter}",
            };

            var nodeChannels = new Dictionary<SharpGLTF.Schema2.Node,
                (List<MeshAssetVectorAnimationKey>, List<MeshAssetQuaternionAnimationKey>, List<MeshAssetVectorAnimationKey>)>();

            foreach (var channel in animation.Channels)
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

            foreach (var pair in nodeChannels)
            {
                var c = new MeshAssetAnimationChannel()
                {
                    nodeIndex = nodeToIndex[pair.Key],
                    positionKeys = pair.Value.Item1.Count > 0 ? pair.Value.Item1 : [new MeshAssetVectorAnimationKey()
                    {
                        time = 0,
                        value = new(pair.Key.LocalTransform.GetDecomposed().Translation),
                    }],
                    rotationKeys = pair.Value.Item2.Count > 0 ? pair.Value.Item2 : [new MeshAssetQuaternionAnimationKey()
                    {
                        time = 0,
                        value = new(pair.Key.LocalTransform.GetDecomposed().Rotation),
                    }],
                    scaleKeys = pair.Value.Item3.Count > 0 ? pair.Value.Item3 : [new MeshAssetVectorAnimationKey()
                    {
                        time = 0,
                        value = new(pair.Key.LocalTransform.GetDecomposed().Scale),
                    }],
                };

                a.channels.Add(c);
            }

            meshData.animations.Add(a);
        }

        return meshData;
    }
}
