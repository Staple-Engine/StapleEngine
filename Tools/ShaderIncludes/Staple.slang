module Staple;

__include StapleMath;
__include StapleLighting;

#ifdef STAPLE_TEXTURE_COUNT
public static const int StapleTextureCount = STAPLE_TEXTURE_COUNT;
#else
public static const int StapleTextureCount = 0;
#endif

#ifdef STAPLE_STORAGE_TEXTURE_COUNT
public static const int StapleStorageTextureStart = StapleTextureCount;
public static const int StapleStorageTextureCount = STAPLE_STORAGE_TEXTURE_COUNT;
#else
public static const int StapleStorageTextureStart = 30;
public static const int StapleStorageTextureCount = 30;
#endif

#ifdef STAPLE_STORAGE_BUFFER_COUNT
public static const int StapleStorageBufferStart = StapleStorageTextureStart + StapleStorageTextureCount;
public static const int StapleStorageBufferCount = StapleStorageBufferStart + STAPLE_STORAGE_BUFFER_COUNT;
#else
public static const int StapleStorageBufferStart = 60;
public static const int StapleStorageBufferCount = 60;
#endif

#ifdef STAPLE_UNIFORM_BUFFER_COUNT
public static const int StapleUniformBufferCount = STAPLE_UNIFORM_BUFFER_COUNT;
#else
public static const int StapleUniformBufferCount = 0;
#endif

#ifdef STAPLE_VERTEX_SHADER
public static const int StapleSamplerStorageBufferSet = 0;
public static const int StapleUniformBufferSet = 1;
#elif defined(STAPLE_FRAGMENT_SHADER)
public static const int StapleSamplerStorageBufferSet = 2;
public static const int StapleUniformBufferSet = 3;
#elif defined(STAPLE_COMPUTE_SHADER)
public static const int StapleSamplerStorageBufferSet = 0;
public static const int StapleReadWriteBufferSet = 1;
public static const int StapleUniformBufferSet = 2;
#else
public static const int StapleSamplerStorageBufferSet = 0;
public static const int StapleReadWriteBufferSet = 0;
public static const int StapleUniformBufferSet = 0;
#endif

public static const int StapleUniformBufferStart = 2;

#ifdef SKINNING
public static const int StapleInternalStorageBufferCount = 1;
#else
public static const int StapleInternalStorageBufferCount = 0;
#endif

[[vk::binding(0, StapleUniformBufferSet)]]
public cbuffer StapleRenderData
{
    public float4x4 world;
    public float4x4 view;
    public float4x4 projection;
};

[[vk::binding(1, StapleUniformBufferSet)]]
public cbuffer StapleFragmentRenderData
{
    public float time;
};

#ifdef SKINNING
[[vk::binding(StapleStorageBufferStart, StapleSamplerStorageBufferSet)]]
public StructuredBuffer<float4> StapleBoneMatrices;
#endif

public float4x4 ProjectionViewWorld(float4x4 world)
{
    return mul(projection, mul(view, world));
}

public float4x4 ViewWorld(float4x4 world)
{
    return mul(view, world);
}

#ifdef SKINNING
public float4x4 StapleGetBoneMatrix(int index)
{
    return transpose(float4x4(StapleBoneMatrices[index * 4],
        StapleBoneMatrices[index * 4 + 1],
        StapleBoneMatrices[index * 4 + 2],
        StapleBoneMatrices[index * 4 + 3]));
}

public float4x4 StapleGetSkinningMatrix(float4x4 world, float4 indices, float4 weights)
{
    return mul(weights.x * StapleGetBoneMatrix(int(indices.x)) +
        weights.y * StapleGetBoneMatrix(int(indices.y)) +
        weights.z * StapleGetBoneMatrix(int(indices.z)) +
        weights.w * StapleGetBoneMatrix(int(indices.w)), world);
}
#endif
