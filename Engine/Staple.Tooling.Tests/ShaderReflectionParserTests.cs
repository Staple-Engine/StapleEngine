using Staple.Internal;
using Staple.Tooling;

namespace StapleToolingTests;

public class ShaderReflectionParserTests
{
    public static readonly string FragmentReflectionData = """
        {
            "parameters": [
                {
                    "name": "Uniforms",
                    "binding": {"kind": "descriptorTableSlot", "space": 3, "index": 2},
                    "type": {
                        "kind": "constantBuffer",
                        "elementType": {
                            "kind": "struct",
                            "fields": [
                                {
                                    "name": "viewPosition",
                                    "type": {
                                        "kind": "vector",
                                        "elementCount": 3,
                                        "elementType": {
                                            "kind": "scalar",
                                            "scalarType": "float32"
                                        }
                                    },
                                    "binding": {"kind": "uniform", "offset": 0, "size": 12, "elementStride": 4}
                                },
                                {
                                    "name": "diffuseColor",
                                    "type": {
                                        "kind": "vector",
                                        "elementCount": 4,
                                        "elementType": {
                                            "kind": "scalar",
                                            "scalarType": "float32"
                                        }
                                    },
                                    "binding": {"kind": "uniform", "offset": 16, "size": 16, "elementStride": 4}
                                },
                                {
                                    "name": "emissiveColor",
                                    "type": {
                                        "kind": "vector",
                                        "elementCount": 4,
                                        "elementType": {
                                            "kind": "scalar",
                                            "scalarType": "float32"
                                        }
                                    },
                                    "binding": {"kind": "uniform", "offset": 32, "size": 16, "elementStride": 4}
                                },
                                {
                                    "name": "specularColor",
                                    "type": {
                                        "kind": "vector",
                                        "elementCount": 4,
                                        "elementType": {
                                            "kind": "scalar",
                                            "scalarType": "float32"
                                        }
                                    },
                                    "binding": {"kind": "uniform", "offset": 48, "size": 16, "elementStride": 4}
                                },
                                {
                                    "name": "cutout",
                                    "type": {
                                        "kind": "scalar",
                                        "scalarType": "float32"
                                    },
                                    "binding": {"kind": "uniform", "offset": 64, "size": 4, "elementStride": 0}
                                },
                                {
                                    "name": "alphaThreshold",
                                    "type": {
                                        "kind": "scalar",
                                        "scalarType": "float32"
                                    },
                                    "binding": {"kind": "uniform", "offset": 68, "size": 4, "elementStride": 0}
                                }
                            ]
                        },
                        "containerVarLayout": {
                            "binding": {"kind": "descriptorTableSlot", "index": 0}
                        },
                        "elementVarLayout": {
                            "type": {
                                "kind": "struct",
                                "fields": [
                                    {
                                        "name": "viewPosition",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 3,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "binding": {"kind": "uniform", "offset": 0, "size": 12, "elementStride": 4}
                                    },
                                    {
                                        "name": "diffuseColor",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 4,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "binding": {"kind": "uniform", "offset": 16, "size": 16, "elementStride": 4}
                                    },
                                    {
                                        "name": "emissiveColor",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 4,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "binding": {"kind": "uniform", "offset": 32, "size": 16, "elementStride": 4}
                                    },
                                    {
                                        "name": "specularColor",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 4,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "binding": {"kind": "uniform", "offset": 48, "size": 16, "elementStride": 4}
                                    },
                                    {
                                        "name": "cutout",
                                        "type": {
                                            "kind": "scalar",
                                            "scalarType": "float32"
                                        },
                                        "binding": {"kind": "uniform", "offset": 64, "size": 4, "elementStride": 0}
                                    },
                                    {
                                        "name": "alphaThreshold",
                                        "type": {
                                            "kind": "scalar",
                                            "scalarType": "float32"
                                        },
                                        "binding": {"kind": "uniform", "offset": 68, "size": 4, "elementStride": 0}
                                    }
                                ]
                            },
                            "binding": {"kind": "uniform", "offset": 0, "size": 80, "elementStride": 0}
                        }
                    }
                },
                {
                    "name": "Textures",
                    "binding": {"kind": "descriptorTableSlot", "space": 2, "index": 0, "count": 2},
                    "type": {
                        "kind": "constantBuffer",
                        "elementType": {
                            "kind": "struct",
                            "fields": [
                                {
                                    "name": "diffuseTexture",
                                    "type": {
                                        "kind": "resource",
                                        "baseShape": "texture2D",
                                        "combined": true,
                                        "resultType": {
                                            "kind": "vector",
                                            "elementCount": 4,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        }
                                    },
                                    "binding": {"kind": "descriptorTableSlot", "index": 0}
                                },
                                {
                                    "name": "normalTexture",
                                    "type": {
                                        "kind": "resource",
                                        "baseShape": "texture2D",
                                        "combined": true,
                                        "resultType": {
                                            "kind": "vector",
                                            "elementCount": 4,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        }
                                    },
                                    "binding": {"kind": "descriptorTableSlot", "index": 1}
                                }
                            ]
                        },
                        "containerVarLayout": {

                        },
                        "elementVarLayout": {
                            "type": {
                                "kind": "struct",
                                "fields": [
                                    {
                                        "name": "diffuseTexture",
                                        "type": {
                                            "kind": "resource",
                                            "baseShape": "texture2D",
                                            "combined": true,
                                            "resultType": {
                                                "kind": "vector",
                                                "elementCount": 4,
                                                "elementType": {
                                                    "kind": "scalar",
                                                    "scalarType": "float32"
                                                }
                                            }
                                        },
                                        "binding": {"kind": "descriptorTableSlot", "index": 0}
                                    },
                                    {
                                        "name": "normalTexture",
                                        "type": {
                                            "kind": "resource",
                                            "baseShape": "texture2D",
                                            "combined": true,
                                            "resultType": {
                                                "kind": "vector",
                                                "elementCount": 4,
                                                "elementType": {
                                                    "kind": "scalar",
                                                    "scalarType": "float32"
                                                }
                                            }
                                        },
                                        "binding": {"kind": "descriptorTableSlot", "index": 1}
                                    }
                                ]
                            },
                            "binding": {"kind": "descriptorTableSlot", "index": 0, "count": 2}
                        }
                    }
                },
                {
                    "name": "StapleRenderData",
                    "binding": {"kind": "descriptorTableSlot", "space": 3, "index": 0},
                    "type": {
                        "kind": "constantBuffer",
                        "elementType": {
                            "kind": "struct",
                            "fields": [
                                {
                                    "name": "world",
                                    "type": {
                                        "kind": "matrix",
                                        "rowCount": 4,
                                        "columnCount": 4,
                                        "elementType": {
                                            "kind": "scalar",
                                            "scalarType": "float32"
                                        }
                                    },
                                    "binding": {"kind": "uniform", "offset": 0, "size": 64, "elementStride": 0}
                                },
                                {
                                    "name": "view",
                                    "type": {
                                        "kind": "matrix",
                                        "rowCount": 4,
                                        "columnCount": 4,
                                        "elementType": {
                                            "kind": "scalar",
                                            "scalarType": "float32"
                                        }
                                    },
                                    "binding": {"kind": "uniform", "offset": 64, "size": 64, "elementStride": 0}
                                },
                                {
                                    "name": "projection",
                                    "type": {
                                        "kind": "matrix",
                                        "rowCount": 4,
                                        "columnCount": 4,
                                        "elementType": {
                                            "kind": "scalar",
                                            "scalarType": "float32"
                                        }
                                    },
                                    "binding": {"kind": "uniform", "offset": 128, "size": 64, "elementStride": 0}
                                },
                                {
                                    "name": "time",
                                    "type": {
                                        "kind": "scalar",
                                        "scalarType": "float32"
                                    },
                                    "binding": {"kind": "uniform", "offset": 192, "size": 4, "elementStride": 0}
                                }
                            ]
                        },
                        "containerVarLayout": {
                            "binding": {"kind": "descriptorTableSlot", "index": 0}
                        },
                        "elementVarLayout": {
                            "type": {
                                "kind": "struct",
                                "fields": [
                                    {
                                        "name": "world",
                                        "type": {
                                            "kind": "matrix",
                                            "rowCount": 4,
                                            "columnCount": 4,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "binding": {"kind": "uniform", "offset": 0, "size": 64, "elementStride": 0}
                                    },
                                    {
                                        "name": "view",
                                        "type": {
                                            "kind": "matrix",
                                            "rowCount": 4,
                                            "columnCount": 4,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "binding": {"kind": "uniform", "offset": 64, "size": 64, "elementStride": 0}
                                    },
                                    {
                                        "name": "projection",
                                        "type": {
                                            "kind": "matrix",
                                            "rowCount": 4,
                                            "columnCount": 4,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "binding": {"kind": "uniform", "offset": 128, "size": 64, "elementStride": 0}
                                    },
                                    {
                                        "name": "time",
                                        "type": {
                                            "kind": "scalar",
                                            "scalarType": "float32"
                                        },
                                        "binding": {"kind": "uniform", "offset": 192, "size": 4, "elementStride": 0}
                                    }
                                ]
                            },
                            "binding": {"kind": "uniform", "offset": 0, "size": 208, "elementStride": 0}
                        }
                    }
                },
                {
                    "name": "StapleBoneMatrices",
                    "binding": {"kind": "descriptorTableSlot", "space": 3, "index": 1},
                    "type": {
                        "kind": "resource",
                        "baseShape": "structuredBuffer",
                        "resultType": {
                            "kind": "vector",
                            "elementCount": 4,
                            "elementType": {
                                "kind": "scalar",
                                "scalarType": "float32"
                            }
                        }
                    }
                }
            ],
            "entryPoints": [
                {
                    "name": "FragmentMain",
                    "stage": "fragment",
                    "parameters": [
                        {
                            "name": "input",
                            "stage": "fragment",
                            "binding": {"kind": "varyingInput", "index": 0, "count": 8},
                            "type": {
                                "kind": "struct",
                                "name": "VertexOutput",
                                "fields": [
                                    {
                                        "name": "position",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 4,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "semanticName": "SV_POSITION"
                                    },
                                    {
                                        "name": "worldPosition",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 3,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "stage": "fragment",
                                        "binding": {"kind": "varyingInput", "index": 0}
                                    },
                                    {
                                        "name": "lightNormal",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 3,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "stage": "fragment",
                                        "binding": {"kind": "varyingInput", "index": 1}
                                    },
                                    {
                                        "name": "coords",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 2,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "stage": "fragment",
                                        "binding": {"kind": "varyingInput", "index": 2}
                                    },
                                    {
                                        "name": "normal",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 3,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "stage": "fragment",
                                        "binding": {"kind": "varyingInput", "index": 3}
                                    },
                                    {
                                        "name": "tangent",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 3,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "stage": "fragment",
                                        "binding": {"kind": "varyingInput", "index": 4}
                                    },
                                    {
                                        "name": "bitangent",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 3,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "stage": "fragment",
                                        "binding": {"kind": "varyingInput", "index": 5}
                                    },
                                    {
                                        "name": "color",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 4,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "stage": "fragment",
                                        "binding": {"kind": "varyingInput", "index": 6}
                                    },
                                    {
                                        "name": "instanceID",
                                        "type": {
                                            "kind": "scalar",
                                            "scalarType": "uint32"
                                        },
                                        "stage": "fragment",
                                        "binding": {"kind": "varyingInput", "index": 7}
                                    }
                                ]
                            }
                        }
                    ],
                    "result": {
                        "stage": "fragment",
                        "binding": {"kind": "varyingOutput", "index": 0},
                        "semanticName": "SV_TARGET",
                        "type": {
                            "kind": "vector",
                            "elementCount": 4,
                            "elementType": {
                                "kind": "scalar",
                                "scalarType": "float32"
                            }
                        }
                    },
                    "bindings": [
                        {
                            "name": "Uniforms",
                            "binding": {"kind": "descriptorTableSlot", "space": 3, "index": 2, "used": 0}
                        },
                        {
                            "name": "Textures",
                            "binding": {"kind": "descriptorTableSlot", "space": 2, "index": 0, "count": 2, "used": 0}
                        },
                        {
                            "name": "StapleRenderData",
                            "binding": {"kind": "descriptorTableSlot", "space": 3, "index": 0, "used": 0}
                        },
                        {
                            "name": "StapleBoneMatrices",
                            "binding": {"kind": "descriptorTableSlot", "space": 3, "index": 1, "used": 0}
                        }
                    ]
                }
            ]
        }
        """;

    [Test]
    public void TestParse()
    {
        var data = ShaderReflectionParser.Parse(FragmentReflectionData);

        Assert.That(data, Is.Not.Null);

        Assert.That(data.textures, Has.Count.EqualTo(2));

        Assert.That(data.textures[0].name, Is.EqualTo("diffuseTexture"));
        Assert.That(data.textures[1].name, Is.EqualTo("normalTexture"));

        Assert.That(data.textures[0].type, Is.EqualTo(ShaderUniformType.Texture));
        Assert.That(data.textures[1].type, Is.EqualTo(ShaderUniformType.Texture));

        Assert.That(data.textures[0].binding, Is.Zero);
        Assert.That(data.textures[1].binding, Is.EqualTo(1));

        Assert.That(data.uniforms, Has.Count.EqualTo(2));

        Assert.That(data.uniforms[0].binding, Is.EqualTo(2));
        Assert.That(data.uniforms[1].binding, Is.Zero);

        Assert.That(data.uniforms[0].name, Is.EqualTo("Uniforms"));
        Assert.That(data.uniforms[1].name, Is.EqualTo("StapleRenderData"));

        Assert.That(data.uniforms[0].fields, Has.Count.EqualTo(6));
        Assert.That(data.uniforms[1].fields, Has.Count.EqualTo(4));

        Assert.That(data.uniforms[0].size, Is.EqualTo(72));
        Assert.That(data.uniforms[1].size, Is.EqualTo(196));

        Assert.That(data.storageBuffers, Has.Count.EqualTo(1));

        Assert.That(data.storageBuffers[0].name, Is.EqualTo("StapleBoneMatrices"));
        Assert.That(data.storageBuffers[0].type, Is.EqualTo(ShaderUniformType.ReadOnlyBuffer));
        Assert.That(data.storageBuffers[0].binding, Is.EqualTo(1));
        Assert.That(data.storageBuffers[0].fields, Has.Count.EqualTo(0));
        Assert.That(data.storageBuffers[0].size, Is.Zero);
    }

    [Test]
    public void TestMerge()
    {
        var data = ShaderReflectionParser.Parse(FragmentReflectionData);
        var copy = ShaderReflectionParser.Parse(FragmentReflectionData);

        Assert.That(data, Is.Not.Null);

        data.Merge(copy);

        Assert.That(data.textures, Has.Count.EqualTo(2));

        Assert.That(data.uniforms, Has.Count.EqualTo(2));

        Assert.That(data.storageBuffers, Has.Count.EqualTo(1));

        var extra = new ShaderUniformContainer();

        extra.uniforms.Add(new()
        {
            binding = 3,
            name = "testFloat",
            type = ShaderUniformType.Float,
        });

        extra.textures.Add(new()
        {
            binding = 3,
            name = "testTexture",
            type = ShaderUniformType.Texture,
        });

        extra.storageBuffers.Add(new()
        {
            binding = 3,
            name = "testBuffer",
            type = ShaderUniformType.ReadOnlyBuffer,
        });

        data.Merge(extra);

        Assert.That(data.textures, Has.Count.EqualTo(3));

        Assert.That(data.uniforms, Has.Count.EqualTo(3));

        Assert.That(data.storageBuffers, Has.Count.EqualTo(2));

        Assert.That(data.uniforms[2].name, Is.EqualTo("testFloat"));
        Assert.That(data.textures[2].name, Is.EqualTo("testTexture"));
        Assert.That(data.storageBuffers[1].name, Is.EqualTo("testBuffer"));
    }
}
