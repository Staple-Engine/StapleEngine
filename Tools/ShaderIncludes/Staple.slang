module Staple;

__include StapleMath;
__include StapleLighting;

#define STAPLE_SKINNING_STAGE_INDEX 14
#define STAPLE_LIGHTING_NORMAL_MATRIX_STAGE_INDEX 13

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
