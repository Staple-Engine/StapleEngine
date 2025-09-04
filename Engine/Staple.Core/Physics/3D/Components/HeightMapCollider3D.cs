using System;
using System.Numerics;

namespace Staple;

public class HeightMapCollider3D : Collider3D
{
    [NonSerialized]
    public float[] heights;
    public Vector3 offset;
    public Vector3 scale;
}
