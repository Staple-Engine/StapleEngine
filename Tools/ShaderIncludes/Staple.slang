module Staple;

__include StapleMath;
__include StapleLighting;

#define STAPLE_SKINNING_STAGE_INDEX 2

#ifdef SKINNING
public static const int StapleBufferIndexCount = 3;
#else
public static const int StapleBufferIndexCount = 2;
#endif

#ifdef STAPLE_VERTEX_SHADER
public static const int StapleSamplerBufferSet = 0;
public static const int StapleUniformBufferSet = 1;
#elif defined(STAPLE_FRAGMENT_SHADER)
public static const int StapleSamplerBufferSet = 2;
public static const int StapleUniformBufferSet = 3;
#elif defined(STAPLE_COMPUTE_SHADER)
public static const int StapleSamplerBufferSet = 0;
public static const int StapleReadWriteBufferSet = 1;
public static const int StapleUniformBufferSet = 2;
#else
public static const int StapleSamplerBufferSet = 0;
public static const int StapleReadWriteBufferSet = 0;
public static const int StapleUniformBufferSet = 0;
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
[[vk::binding(STAPLE_SKINNING_STAGE_INDEX, StapleUniformBufferSet)]]
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
    return float4x4(StapleBoneMatrices[index * 4],
        StapleBoneMatrices[index * 4 + 1],
        StapleBoneMatrices[index * 4 + 2],
        StapleBoneMatrices[index * 4 + 3]);
}

public float4x4 StapleGetSkinningMatrix(float4x4 model, float4 indices, float4 weights)
{
    return model * (weights.x * StapleGetBoneMatrix(int(indices.x)) +
        weights.y * StapleGetBoneMatrix(int(indices.y)) +
        weights.z * StapleGetBoneMatrix(int(indices.z)) +
        weights.w * StapleGetBoneMatrix(int(indices.w)));
}
#endif
