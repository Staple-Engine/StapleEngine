module Staple;

__include StapleMath;
__include StapleInternal;
__include StapleLighting;

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
    return mul(world, weights.x * StapleGetBoneMatrix(int(indices.x)) +
        weights.y * StapleGetBoneMatrix(int(indices.y)) +
        weights.z * StapleGetBoneMatrix(int(indices.z)) +
        weights.w * StapleGetBoneMatrix(int(indices.w)));
}
#endif
