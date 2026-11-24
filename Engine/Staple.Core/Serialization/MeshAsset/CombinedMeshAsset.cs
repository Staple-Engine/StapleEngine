using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple;

public partial class CombinedMeshAsset : IStapleAsset
{
    [Serializable]
    public class MeshData
    {
        [SerializeAsHex]
        public Vector3[] vertices;

        [SerializeAsHex]
        public Vector3[] normals;

        [SerializeAsHex]
        public Vector3[] tangents;

        [SerializeAsHex]
        public Vector3[] bitangents;

        [SerializeAsHex]
        public Color[] colors;

        [SerializeAsHex]
        public Color[] colors2;

        [SerializeAsHex]
        public Color[] colors3;

        [SerializeAsHex]
        public Color[] colors4;

        [SerializeAsHex]
        public Vector2[] uv;

        [SerializeAsHex]
        public Vector2[] uv2;

        [SerializeAsHex]
        public Vector2[] uv3;

        [SerializeAsHex]
        public Vector2[] uv4;

        [SerializeAsHex]
        public Vector2[] uv5;

        [SerializeAsHex]
        public Vector2[] uv6;

        [SerializeAsHex]
        public Vector2[] uv7;

        [SerializeAsHex]
        public Vector2[] uv8;

        [SerializeAsHex]
        public int[] indices;
        public MeshTopology meshTopology;
        public string materialGuid;

        [NonSerialized]
        public MeshAssetComponent Components
        {
            get
            {
                var result = MeshAssetComponent.None;

                if ((uv?.Length ?? 0) > 0)
                {
                    result |= MeshAssetComponent.UV1;
                }

                if ((uv2?.Length ?? 0) > 0)
                {
                    result |= MeshAssetComponent.UV2;
                }

                if ((uv3?.Length ?? 0) > 0)
                {
                    result |= MeshAssetComponent.UV3;
                }

                if ((uv4?.Length ?? 0) > 0)
                {
                    result |= MeshAssetComponent.UV4;
                }

                if ((uv5?.Length ?? 0) > 0)
                {
                    result |= MeshAssetComponent.UV5;
                }

                if ((uv6?.Length ?? 0) > 0)
                {
                    result |= MeshAssetComponent.UV6;
                }

                if ((uv7?.Length ?? 0) > 0)
                {
                    result |= MeshAssetComponent.UV7;
                }

                if ((uv8?.Length ?? 0) > 0)
                {
                    result |= MeshAssetComponent.UV8;
                }

                if ((normals?.Length ?? 0) > 0)
                {
                    result |= MeshAssetComponent.Normal;
                }

                if ((tangents?.Length ?? 0) > 0)
                {
                    result |= MeshAssetComponent.Tangent;
                }

                if ((bitangents?.Length ?? 0) > 0)
                {
                    result |= MeshAssetComponent.Bitangent;
                }

                if ((colors?.Length ?? 0) > 0)
                {
                    result |= MeshAssetComponent.Color1;
                }

                if ((colors2?.Length ?? 0) > 0)
                {
                    result |= MeshAssetComponent.Color2;
                }

                if ((colors3?.Length ?? 0) > 0)
                {
                    result |= MeshAssetComponent.Color3;
                }

                if ((colors4?.Length ?? 0) > 0)
                {
                    result |= MeshAssetComponent.Color4;
                }

                return result;
            }
        }

        public Mesh ToMesh()
        {
            return new Mesh(true, false)
            {
                VerticesInternal = vertices,
                UVInternal = uv,
                UV2Internal = uv2,
                UV3Internal = uv3,
                UV4Internal = uv4,
                UV5Internal = uv5,
                UV6Internal = uv6,
                UV7Internal = uv7,
                UV8Internal = uv8,
                NormalsInternal = normals,
                TangentsInternal = tangents,
                BitangentsInternal = bitangents,
                ColorsInternal = colors,
                Colors2Internal = colors2,
                Colors3Internal = colors3,
                Colors4Internal = colors4,
            };
        }
    }

    public List<MeshData> entries = [];

    public Vector3 combinedBoundsCenter;

    public Vector3 combinedBoundsSize;
}
