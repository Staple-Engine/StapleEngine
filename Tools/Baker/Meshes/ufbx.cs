using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace UFBX;

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public struct UFBXTransform
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public unsafe struct UFBXMeshBone
{
    public int nodeIndex;
    public Matrix4x4 offsetMatrix;
}

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public unsafe struct UFBXMesh
{
    public Vector3* vertices;
    public Vector3* normals;
    public Vector3* tangents;
    public Vector3* bitangents;
    public Vector2* uv0;
    public Vector2* uv1;
    public Vector2* uv2;
    public Vector2* uv3;
    public Vector2* uv4;
    public Vector2* uv5;
    public Vector2* uv6;
    public Vector2* uv7;
    public Vector4* color0;
    public Vector4* color1;
    public Vector4* color2;
    public Vector4* color3;
    public Vector4* boneIndices;
    public Vector4* boneWeights;

    public int vertexCount;

    public uint* indices;
    public int indexCount;

    public int materialIndex;

    [MarshalAs(UnmanagedType.I1)]
    public bool isSkinned;

    public UFBXMeshBone* bones;

    public int boneCount;

    public readonly Span<Vector3> Vertices => vertexCount > 0 ? new(vertices, vertexCount) : default;

    public readonly Span<Vector3> Normals => vertexCount > 0 ? new(normals, vertexCount) : default;

    public readonly Span<Vector3> Tangents => vertexCount > 0 && tangents != null ? new(tangents, vertexCount) : default;

    public readonly Span<Vector3> Bitangents => vertexCount > 0 && bitangents != null ? new(bitangents, vertexCount) : default;

    public readonly Span<Vector2> UV0 => vertexCount > 0 && uv0 != null ? new(uv0, vertexCount) : default;

    public readonly Span<Vector2> UV1 => vertexCount > 0 && uv1 != null ? new(uv1, vertexCount) : default;

    public readonly Span<Vector2> UV2 => vertexCount > 0 && uv2 != null ? new(uv2, vertexCount) : default;

    public readonly Span<Vector2> UV3 => vertexCount > 0 && uv3 != null ? new(uv3, vertexCount) : default;

    public readonly Span<Vector2> UV4 => vertexCount > 0 && uv4 != null ? new(uv4, vertexCount) : default;

    public readonly Span<Vector2> UV5 => vertexCount > 0 && uv5 != null ? new(uv5, vertexCount) : default;

    public readonly Span<Vector2> UV6 => vertexCount > 0 && uv6 != null ? new(uv6, vertexCount) : default;

    public readonly Span<Vector2> UV7 => vertexCount > 0 && uv7 != null ? new(uv7, vertexCount) : default;

    public readonly Span<Vector4> Color0 => vertexCount > 0 && color0 != null ? new(color0, vertexCount) : default;

    public readonly Span<Vector4> Color1 => vertexCount > 0 && color1 != null ? new(color1, vertexCount) : default;

    public readonly Span<Vector4> Color2 => vertexCount > 0 && color2 != null ? new(color2, vertexCount) : default;

    public readonly Span<Vector4> Color3 => vertexCount > 0 && color3 != null ? new(color3, vertexCount) : default;

    public readonly Span<Vector4> BoneIndices => vertexCount > 0 && boneIndices != null ? new(boneIndices, vertexCount) : default;

    public readonly Span<Vector4> BoneWeights => vertexCount > 0 && boneWeights != null ? new(boneWeights, vertexCount) : default;

    public readonly Span<uint> Indices => indexCount > 0 ? new(indices, indexCount) : default;

    public readonly Span<UFBXMeshBone> Bones => boneCount > 0 ? new(bones, boneCount) : default;
}

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public unsafe struct UFBXString
{
    public fixed sbyte data[10240];
    public int length;

    public readonly string Value
    {
        get
        {
            unsafe
            {
                fixed (void* ptr = data)
                {
                    return Encoding.UTF8.GetString((byte*)ptr, length);
                }
            }
        }
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public unsafe struct UFBXMaterial
{
    public UFBXString name;

    public Vector4 diffuseColor;
    public UFBXString diffuseTexture;
    public int diffuseWrapU;
    public int diffuseWrapV;

    public Vector4 specularColor;
    public UFBXString specularTexture;
    public int specularWrapU;
    public int specularWrapV;

    public Vector4 reflectionColor;
    public UFBXString reflectionTexture;
    public int reflectionWrapU;
    public int reflectionWrapV;

    public Vector4 transparencyColor;
    public UFBXString transparencyTexture;
    public int transparencyWrapU;
    public int transparencyWrapV;

    public Vector4 emissionColor;
    public UFBXString emissionTexture;
    public int emissionWrapU;
    public int emissionWrapV;

    public Vector4 ambientColor;
    public UFBXString ambientTexture;
    public int ambientWrapU;
    public int ambientWrapV;

    public Vector4 normalMapColor;
    public UFBXString normalMapTexture;
    public int normalMapWrapU;
    public int normalMapWrapV;

    public Vector4 bumpColor;
    public UFBXString bumpTexture;
    public int bumpWrapU;
    public int bumpWrapV;

    public Vector4 displacementColor;
    public UFBXString displacementTexture;
    public int displacementWrapU;
    public int displacementWrapV;

    public Vector4 vectorDisplacementColor;
    public UFBXString vectorDisplacementTexture;
    public int vectorDisplacementWrapU;
    public int vectorDisplacementWrapV;
}

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public unsafe struct UFBXNode
{
    public int parentIndex;

    public UFBXString name;

    public int* meshIndices;
    public int meshCount;

    public UFBXTransform localTransform;

    public Matrix4x4 geometryToNode;
    public Matrix4x4 nodeToParent;
    public Matrix4x4 nodeToWorld;
    public Matrix4x4 geometryToWorld;
    public Matrix4x4 normalToWorld;

    public readonly Span<int> MeshIndices => meshCount > 0 ? new(meshIndices, meshCount) : default;
}

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public unsafe struct UFBXVector3Key
{
    public float time;
    public Vector3 value;
}

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public unsafe struct UFBXQuaternionKey
{
    public float time;
    public Quaternion value;
}

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public unsafe struct UFBXNodeAnimation
{
    public int nodeIndex;
    public UFBXVector3Key* positions;
    public UFBXQuaternionKey* rotations;
    public UFBXVector3Key* scales;

    public int positionCount;
    public int rotationCount;
    public int scaleCount;

    public readonly Span<UFBXVector3Key> Positions => positionCount > 0 ? new(positions, positionCount) : default;

    public readonly Span<UFBXQuaternionKey> Rotations => rotationCount > 0 ? new(rotations, rotationCount) : default;

    public readonly Span<UFBXVector3Key> Scales => scaleCount > 0 ? new(scales, scaleCount) : default;
}

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public unsafe struct UFBXAnimation
{
    public UFBXString name;
    public float duration;

    public UFBXNodeAnimation* nodes;
    public int nodeCount;
}

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public unsafe struct UFBXScene
{
    public UFBXNode* nodes;
    public int nodeCount;

    public UFBXMesh* meshes;
    public int meshCount;

    public UFBXMaterial* materials;
    public int materialCount;

    public UFBXAnimation* animations;
    public int animationCount;

    public readonly Span<UFBXNode> Nodes => nodeCount > 0 ? new(nodes, nodeCount) : default;

    public readonly Span<UFBXMesh> Meshes => meshCount > 0 ? new(meshes, meshCount) : default;

    public readonly Span<UFBXMaterial> Materials => materialCount > 0 ? new(materials, materialCount) : default;

    public readonly Span<UFBXAnimation> Animations => animationCount > 0 ? new(animations, animationCount) : default;
}

public partial class UFBX
{
    [LibraryImport("StapleToolingSupport", EntryPoint = "UFBXLoadScene", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe partial UFBXScene* LoadScene(string fileName);

    [LibraryImport("StapleToolingSupport", EntryPoint = "UFBXFreeScene", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe partial void FreeScene(UFBXScene* scene);
}
