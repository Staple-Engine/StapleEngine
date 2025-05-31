using System;
using System.Collections;
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
public unsafe struct UFBXNode
{
    public int parentIndex;

    public fixed sbyte name[10240];
    public int nameLength;

    public UFBXTransform localTransform;

    public Matrix4x4 geometryToNode;
    public Matrix4x4 nodeToParent;
    public Matrix4x4 nodeToWorld;
    public Matrix4x4 geometryToWorld;
    public Matrix4x4 normalToWorld;

    public readonly string Name
    {
        get
        {
            unsafe
            {
                fixed(void *ptr = name)
                {
                    return Encoding.UTF8.GetString((byte*)ptr, nameLength);
                }
            }
        }
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public unsafe struct UFBXScene
{
    public UFBXNode* nodes;
    public ulong nodeCount;

    public UFBXNode this[int index]
    {
        get
        {
            return nodes[index];
        }
    }
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
