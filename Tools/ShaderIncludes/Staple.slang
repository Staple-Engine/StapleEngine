module Staple;

__include StapleMath;

#define STAPLE_SKINNING_STAGE_INDEX 15
#define STAPLE_LIGHTING_NORMAL_MATRIX_STAGE_INDEX 14

public cbuffer StapleRenderData
{
    public float4x4 world;
    public float4x4 view;
    public float4x4 projection;
    public float time;
};

#ifdef SKINNING
public layout(binding = STAPLE_SKINNING_STAGE_INDEX) StructuredBuffer<float4> StapleBoneMatrices;
#endif
