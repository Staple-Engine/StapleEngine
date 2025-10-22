module Staple;

__include StapleMath;
__include StapleLighting;

#define STAPLE_SKINNING_STAGE_INDEX 15
#define STAPLE_LIGHTING_NORMAL_MATRIX_STAGE_INDEX 14

#ifdef STAPLE_VERTEX_SHADER
[[vk::binding(0, 1)]]
#endif

#ifdef STAPLE_FRAGMENT_SHADER
[[vk::binding(0, 3)]]
#endif
public cbuffer StapleRenderData
{
    public float4x4 world;
    public float4x4 view;
    public float4x4 projection;
    public float time;
};

#ifdef SKINNING
#   ifdef STAPLE_VERTEX_SHADER
[[vk::binding(STAPLE_SKINNING_STAGE_INDEX, 1)]]
#   endif

#   ifdef STAPLE_FRAGMENT_SHADER
[[vk::binding(STAPLE_SKINNING_STAGE_INDEX, 3)]]
#   endif
public StructuredBuffer<float4> StapleBoneMatrices;
#endif
