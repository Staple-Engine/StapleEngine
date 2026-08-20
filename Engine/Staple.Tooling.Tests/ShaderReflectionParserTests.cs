using Staple;
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
    public static readonly string VertexReflectionData = """
        {
            "parameters": [
                {
                    "name": "Uniforms",
                    "binding": {"kind": "descriptorTableSlot", "space": 1, "index": 1},
                    "type": {
                        "kind": "constantBuffer",
                        "elementType": {
                            "kind": "struct",
                            "fields": [
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
                                    "binding": {"kind": "uniform", "offset": 0, "size": 16, "elementStride": 4}
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
                                    "binding": {"kind": "uniform", "offset": 16, "size": 16, "elementStride": 4}
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
                                    "binding": {"kind": "uniform", "offset": 32, "size": 16, "elementStride": 4}
                                },
                                {
                                    "name": "alphaThreshold",
                                    "type": {
                                        "kind": "scalar",
                                        "scalarType": "float32"
                                    },
                                    "binding": {"kind": "uniform", "offset": 48, "size": 4, "elementStride": 0}
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
                                        "name": "diffuseColor",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 4,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "binding": {"kind": "uniform", "offset": 0, "size": 16, "elementStride": 4}
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
                                        "binding": {"kind": "uniform", "offset": 16, "size": 16, "elementStride": 4}
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
                                        "binding": {"kind": "uniform", "offset": 32, "size": 16, "elementStride": 4}
                                    },
                                    {
                                        "name": "alphaThreshold",
                                        "type": {
                                            "kind": "scalar",
                                            "scalarType": "float32"
                                        },
                                        "binding": {"kind": "uniform", "offset": 48, "size": 4, "elementStride": 0}
                                    }
                                ]
                            },
                            "binding": {"kind": "uniform", "offset": 0, "size": 64, "elementStride": 0}
                        }
                    }
                },
                {
                    "name": "StapleRenderData",
                    "binding": {"kind": "descriptorTableSlot", "space": 1, "index": 0},
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
                                    "name": "useWorldMatrix",
                                    "type": {
                                        "kind": "scalar",
                                        "scalarType": "bool"
                                    },
                                    "binding": {"kind": "uniform", "offset": 192, "size": 4, "elementStride": 0}
                                },
                                {
                                    "name": "renderQueue",
                                    "type": {
                                        "kind": "scalar",
                                        "scalarType": "int32"
                                    },
                                    "binding": {"kind": "uniform", "offset": 196, "size": 4, "elementStride": 0}
                                },
                                {
                                    "name": "time",
                                    "type": {
                                        "kind": "scalar",
                                        "scalarType": "float32"
                                    },
                                    "binding": {"kind": "uniform", "offset": 200, "size": 4, "elementStride": 0}
                                },
                                {
                                    "name": "padding0",
                                    "type": {
                                        "kind": "scalar",
                                        "scalarType": "float32"
                                    },
                                    "binding": {"kind": "uniform", "offset": 204, "size": 4, "elementStride": 0}
                                },
                                {
                                    "name": "StapleLightCountViewPosition",
                                    "type": {
                                        "kind": "vector",
                                        "elementCount": 4,
                                        "elementType": {
                                            "kind": "scalar",
                                            "scalarType": "float32"
                                        }
                                    },
                                    "binding": {"kind": "uniform", "offset": 208, "size": 16, "elementStride": 4}
                                },
                                {
                                    "name": "StapleLightAmbientColor",
                                    "type": {
                                        "kind": "vector",
                                        "elementCount": 4,
                                        "elementType": {
                                            "kind": "scalar",
                                            "scalarType": "float32"
                                        }
                                    },
                                    "binding": {"kind": "uniform", "offset": 224, "size": 16, "elementStride": 4}
                                },
                                {
                                    "name": "StapleLightTypePosition",
                                    "type": {
                                        "kind": "array",
                                        "elementCount": 16,
                                        "elementType": {
                                            "kind": "vector",
                                            "elementCount": 4,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "uniformStride": 16
                                    },
                                    "binding": {"kind": "uniform", "offset": 240, "size": 256, "elementStride": 16}
                                },
                                {
                                    "name": "StapleLightDiffuse",
                                    "type": {
                                        "kind": "array",
                                        "elementCount": 16,
                                        "elementType": {
                                            "kind": "vector",
                                            "elementCount": 4,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "uniformStride": 16
                                    },
                                    "binding": {"kind": "uniform", "offset": 496, "size": 256, "elementStride": 16}
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
                                        "name": "useWorldMatrix",
                                        "type": {
                                            "kind": "scalar",
                                            "scalarType": "bool"
                                        },
                                        "binding": {"kind": "uniform", "offset": 192, "size": 4, "elementStride": 0}
                                    },
                                    {
                                        "name": "renderQueue",
                                        "type": {
                                            "kind": "scalar",
                                            "scalarType": "int32"
                                        },
                                        "binding": {"kind": "uniform", "offset": 196, "size": 4, "elementStride": 0}
                                    },
                                    {
                                        "name": "time",
                                        "type": {
                                            "kind": "scalar",
                                            "scalarType": "float32"
                                        },
                                        "binding": {"kind": "uniform", "offset": 200, "size": 4, "elementStride": 0}
                                    },
                                    {
                                        "name": "padding0",
                                        "type": {
                                            "kind": "scalar",
                                            "scalarType": "float32"
                                        },
                                        "binding": {"kind": "uniform", "offset": 204, "size": 4, "elementStride": 0}
                                    },
                                    {
                                        "name": "StapleLightCountViewPosition",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 4,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "binding": {"kind": "uniform", "offset": 208, "size": 16, "elementStride": 4}
                                    },
                                    {
                                        "name": "StapleLightAmbientColor",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 4,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "binding": {"kind": "uniform", "offset": 224, "size": 16, "elementStride": 4}
                                    },
                                    {
                                        "name": "StapleLightTypePosition",
                                        "type": {
                                            "kind": "array",
                                            "elementCount": 16,
                                            "elementType": {
                                                "kind": "vector",
                                                "elementCount": 4,
                                                "elementType": {
                                                    "kind": "scalar",
                                                    "scalarType": "float32"
                                                }
                                            },
                                            "uniformStride": 16
                                        },
                                        "binding": {"kind": "uniform", "offset": 240, "size": 256, "elementStride": 16}
                                    },
                                    {
                                        "name": "StapleLightDiffuse",
                                        "type": {
                                            "kind": "array",
                                            "elementCount": 16,
                                            "elementType": {
                                                "kind": "vector",
                                                "elementCount": 4,
                                                "elementType": {
                                                    "kind": "scalar",
                                                    "scalarType": "float32"
                                                }
                                            },
                                            "uniformStride": 16
                                        },
                                        "binding": {"kind": "uniform", "offset": 496, "size": 256, "elementStride": 16}
                                    }
                                ]
                            },
                            "binding": {"kind": "uniform", "offset": 0, "size": 752, "elementStride": 0}
                        }
                    }
                },
                {
                    "name": "StapleEntityTransforms",
                    "binding": {"kind": "descriptorTableSlot", "index": 60},
                    "type": {
                        "kind": "resource",
                        "baseShape": "structuredBuffer",
                        "resultType": {
                            "kind": "matrix",
                            "rowCount": 4,
                            "columnCount": 4,
                            "elementType": {
                                "kind": "scalar",
                                "scalarType": "float32"
                            }
                        }
                    }
                },
                {
                    "name": "StapleEntityTransformIDs",
                    "binding": {"kind": "descriptorTableSlot", "index": 61},
                    "type": {
                        "kind": "resource",
                        "baseShape": "structuredBuffer",
                        "resultType": {
                            "kind": "scalar",
                            "scalarType": "uint32"
                        }
                    }
                },
                {
                    "name": "StapleBoneMatrices",
                    "binding": {"kind": "descriptorTableSlot", "index": 62},
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
                },
                {
                    "name": "StapleBlendShapeData",
                    "binding": {"kind": "descriptorTableSlot", "index": 63},
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
                },
                {
                    "name": "StapleBlendShapeParameters",
                    "binding": {"kind": "descriptorTableSlot", "index": 64},
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
                    "name": "VertexMain",
                    "stage": "vertex",
                    "parameters": [
                        {
                            "name": "input",
                            "stage": "vertex",
                            "binding": {"kind": "varyingInput", "index": 0, "count": 11},
                            "type": {
                                "kind": "struct",
                                "name": "Input",
                                "fields": [
                                    {
                                        "name": "position",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 3,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "stage": "vertex",
                                        "binding": {"kind": "varyingInput", "index": 0},
                                        "semanticName": "POSITION"
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
                                        "stage": "vertex",
                                        "binding": {"kind": "varyingInput", "index": 1},
                                        "semanticName": "TEXCOORD"
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
                                        "stage": "vertex",
                                        "binding": {"kind": "varyingInput", "index": 2},
                                        "semanticName": "NORMAL"
                                    },
                                    {
                                        "name": "coords1",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 2,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "stage": "vertex",
                                        "binding": {"kind": "varyingInput", "index": 3},
                                        "semanticName": "TEXCOORD",
                                        "semanticIndex": 1
                                    },
                                    {
                                        "name": "coords3",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 2,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "stage": "vertex",
                                        "binding": {"kind": "varyingInput", "index": 4},
                                        "semanticName": "TEXCOORD",
                                        "semanticIndex": 3
                                    },
                                    {
                                        "name": "coords4",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 2,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float16"
                                            }
                                        },
                                        "stage": "vertex",
                                        "binding": {"kind": "varyingInput", "index": 5},
                                        "semanticName": "TEXCOORD",
                                        "semanticIndex": 4
                                    },
                                    {
                                        "name": "color1",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 4,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "stage": "vertex",
                                        "binding": {"kind": "varyingInput", "index": 6},
                                        "semanticName": "COLOR",
                                        "semanticIndex": 1
                                    },
                                    {
                                        "name": "color2",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 4,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "stage": "vertex",
                                        "binding": {"kind": "varyingInput", "index": 7},
                                        "semanticName": "COLOR",
                                        "semanticIndex": 2
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
                                        "stage": "vertex",
                                        "binding": {"kind": "varyingInput", "index": 8},
                                        "semanticName": "COLOR"
                                    },
                                    {
                                        "name": "indices",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 4,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "stage": "vertex",
                                        "binding": {"kind": "varyingInput", "index": 9},
                                        "semanticName": "BLENDINDICES"
                                    },
                                    {
                                        "name": "weights",
                                        "type": {
                                            "kind": "vector",
                                            "elementCount": 4,
                                            "elementType": {
                                                "kind": "scalar",
                                                "scalarType": "float32"
                                            }
                                        },
                                        "stage": "vertex",
                                        "binding": {"kind": "varyingInput", "index": 10},
                                        "semanticName": "BLENDWEIGHTS"
                                    },
                                    {
                                        "name": "baseInstance",
                                        "type": {
                                            "kind": "scalar",
                                            "scalarType": "uint32"
                                        },
                                        "semanticName": "SV_STARTINSTANCELOCATION"
                                    },
                                    {
                                        "name": "instanceID",
                                        "type": {
                                            "kind": "scalar",
                                            "scalarType": "uint32"
                                        },
                                        "semanticName": "SV_INSTANCEID"
                                    },
                                    {
                                        "name": "baseVertex",
                                        "type": {
                                            "kind": "scalar",
                                            "scalarType": "uint32"
                                        },
                                        "semanticName": "SV_STARTVERTEXLOCATION"
                                    },
                                    {
                                        "name": "vertexID",
                                        "type": {
                                            "kind": "scalar",
                                            "scalarType": "uint32"
                                        },
                                        "semanticName": "SV_VERTEXID"
                                    }
                                ]
                            }
                        }
                    ],
                    "result": {
                        "stage": "vertex",
                        "binding": {"kind": "varyingOutput", "index": 0, "count": 6},
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
                                    "stage": "vertex",
                                    "binding": {"kind": "varyingOutput", "index": 0}
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
                                    "stage": "vertex",
                                    "binding": {"kind": "varyingOutput", "index": 1}
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
                                    "stage": "vertex",
                                    "binding": {"kind": "varyingOutput", "index": 2}
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
                                    "stage": "vertex",
                                    "binding": {"kind": "varyingOutput", "index": 3}
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
                                    "stage": "vertex",
                                    "binding": {"kind": "varyingOutput", "index": 4}
                                },
                                {
                                    "name": "instanceID",
                                    "type": {
                                        "kind": "scalar",
                                        "scalarType": "uint32"
                                    },
                                    "stage": "vertex",
                                    "binding": {"kind": "varyingOutput", "index": 5}
                                }
                            ]
                        }
                    },
                    "bindings": [
                        {
                            "name": "Uniforms",
                            "binding": {"kind": "descriptorTableSlot", "space": 1, "index": 1, "used": 1}
                        },
                        {
                            "name": "StapleRenderData",
                            "binding": {"kind": "descriptorTableSlot", "space": 1, "index": 0, "used": 1}
                        },
                        {
                            "name": "StapleEntityTransforms",
                            "binding": {"kind": "descriptorTableSlot", "index": 60, "used": 1}
                        },
                        {
                            "name": "StapleEntityTransformIDs",
                            "binding": {"kind": "descriptorTableSlot", "index": 61, "used": 1}
                        },
                        {
                            "name": "StapleBoneMatrices",
                            "binding": {"kind": "descriptorTableSlot", "index": 62, "used": 1}
                        },
                        {
                            "name": "StapleBlendShapeData",
                            "binding": {"kind": "descriptorTableSlot", "index": 63, "used": 1}
                        },
                        {
                            "name": "StapleBlendShapeParameters",
                            "binding": {"kind": "descriptorTableSlot", "index": 64, "used": 1}
                        }
                    ]
                }
            ],
            "bindlessSpaceIndex": 2
        }
        """;

    [Test]
    public void TestParseFragment()
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

        Assert.That(data.uniforms[0].fields, Has.Length.EqualTo(6));
        Assert.That(data.uniforms[1].fields, Has.Length.EqualTo(4));

        Assert.That(data.uniforms[0].size, Is.EqualTo(72));
        Assert.That(data.uniforms[1].size, Is.EqualTo(196));

        Assert.That(data.storageBuffers, Has.Count.EqualTo(1));

        Assert.That(data.storageBuffers[0].name, Is.EqualTo("StapleBoneMatrices"));
        Assert.That(data.storageBuffers[0].type, Is.EqualTo(ShaderUniformType.ReadOnlyBuffer));
        Assert.That(data.storageBuffers[0].binding, Is.EqualTo(1));
        Assert.That(data.storageBuffers[0].fields, Is.Null);
        Assert.That(data.storageBuffers[0].size, Is.Zero);
    }

    [Test]
    public void TestParseVertex()
    {
        var data = ShaderReflectionParser.Parse(VertexReflectionData);

        Assert.That(data, Is.Not.Null);

        Assert.That(data.textures, Has.Count.EqualTo(0));

        Assert.That(data.uniforms, Has.Count.EqualTo(2));

        Assert.That(data.uniforms[0].binding, Is.EqualTo(1));
        Assert.That(data.uniforms[1].binding, Is.Zero);

        Assert.That(data.uniforms[0].name, Is.EqualTo("Uniforms"));
        Assert.That(data.uniforms[1].name, Is.EqualTo("StapleRenderData"));

        Assert.That(data.uniforms[0].fields, Has.Length.EqualTo(4));
        Assert.That(data.uniforms[1].fields, Has.Length.EqualTo(11));

        Assert.That(data.uniforms[0].size, Is.EqualTo(52));
        Assert.That(data.uniforms[1].size, Is.EqualTo(752));

        Assert.That(data.storageBuffers, Has.Count.EqualTo(5));

        Assert.That(data.storageBuffers[0].name, Is.EqualTo("StapleEntityTransforms"));
        Assert.That(data.storageBuffers[0].type, Is.EqualTo(ShaderUniformType.ReadOnlyBuffer));
        Assert.That(data.storageBuffers[0].binding, Is.EqualTo(60));
        Assert.That(data.storageBuffers[0].fields, Is.Null);
        Assert.That(data.storageBuffers[0].size, Is.Zero);

        Assert.That(data.storageBuffers[1].name, Is.EqualTo("StapleEntityTransformIDs"));
        Assert.That(data.storageBuffers[1].type, Is.EqualTo(ShaderUniformType.ReadOnlyBuffer));
        Assert.That(data.storageBuffers[1].binding, Is.EqualTo(61));
        Assert.That(data.storageBuffers[1].fields, Is.Null);
        Assert.That(data.storageBuffers[1].size, Is.Zero);

        Assert.That(data.storageBuffers[2].name, Is.EqualTo("StapleBoneMatrices"));
        Assert.That(data.storageBuffers[2].type, Is.EqualTo(ShaderUniformType.ReadOnlyBuffer));
        Assert.That(data.storageBuffers[2].binding, Is.EqualTo(62));
        Assert.That(data.storageBuffers[2].fields, Is.Null);
        Assert.That(data.storageBuffers[2].size, Is.Zero);

        Assert.That(data.storageBuffers[3].name, Is.EqualTo("StapleBlendShapeData"));
        Assert.That(data.storageBuffers[3].type, Is.EqualTo(ShaderUniformType.ReadOnlyBuffer));
        Assert.That(data.storageBuffers[3].binding, Is.EqualTo(63));
        Assert.That(data.storageBuffers[3].fields, Is.Null);
        Assert.That(data.storageBuffers[3].size, Is.Zero);

        Assert.That(data.storageBuffers[4].name, Is.EqualTo("StapleBlendShapeParameters"));
        Assert.That(data.storageBuffers[4].type, Is.EqualTo(ShaderUniformType.ReadOnlyBuffer));
        Assert.That(data.storageBuffers[4].binding, Is.EqualTo(64));
        Assert.That(data.storageBuffers[4].fields, Is.Null);
        Assert.That(data.storageBuffers[4].size, Is.Zero);

        Assert.That(data.vertexAttributes, Has.Count.EqualTo(11));

        Assert.That(data.vertexAttributes[0].attribute, Is.EqualTo(VertexAttribute.Position));
        Assert.That(data.vertexAttributes[0].attributeType, Is.EqualTo(VertexAttributeType.Float3));

        Assert.That(data.vertexAttributes[1].attribute, Is.EqualTo(VertexAttribute.TexCoord0));
        Assert.That(data.vertexAttributes[1].attributeType, Is.EqualTo(VertexAttributeType.Float2));

        Assert.That(data.vertexAttributes[2].attribute, Is.EqualTo(VertexAttribute.Normal));
        Assert.That(data.vertexAttributes[2].attributeType, Is.EqualTo(VertexAttributeType.Float3));

        Assert.That(data.vertexAttributes[3].attribute, Is.EqualTo(VertexAttribute.TexCoord1));
        Assert.That(data.vertexAttributes[3].attributeType, Is.EqualTo(VertexAttributeType.Float2));

        Assert.That(data.vertexAttributes[4].attribute, Is.EqualTo(VertexAttribute.TexCoord3));
        Assert.That(data.vertexAttributes[4].attributeType, Is.EqualTo(VertexAttributeType.Float2));

        Assert.That(data.vertexAttributes[5].attribute, Is.EqualTo(VertexAttribute.TexCoord4));
        Assert.That(data.vertexAttributes[5].attributeType, Is.EqualTo(VertexAttributeType.Half2));

        Assert.That(data.vertexAttributes[6].attribute, Is.EqualTo(VertexAttribute.Color1));
        Assert.That(data.vertexAttributes[6].attributeType, Is.EqualTo(VertexAttributeType.Float4));

        Assert.That(data.vertexAttributes[7].attribute, Is.EqualTo(VertexAttribute.Color2));
        Assert.That(data.vertexAttributes[7].attributeType, Is.EqualTo(VertexAttributeType.Float4));

        Assert.That(data.vertexAttributes[8].attribute, Is.EqualTo(VertexAttribute.Color0));
        Assert.That(data.vertexAttributes[8].attributeType, Is.EqualTo(VertexAttributeType.Float4));

        Assert.That(data.vertexAttributes[9].attribute, Is.EqualTo(VertexAttribute.BlendIndices));
        Assert.That(data.vertexAttributes[9].attributeType, Is.EqualTo(VertexAttributeType.Float4));

        Assert.That(data.vertexAttributes[10].attribute, Is.EqualTo(VertexAttribute.BlendWeights));
        Assert.That(data.vertexAttributes[10].attributeType, Is.EqualTo(VertexAttributeType.Float4));
    }
}
