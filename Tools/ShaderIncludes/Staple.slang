module Staple;

__include StapleMath;
__include StapleInternal;
__include StapleLighting;

public enum RenderQueue
{
    Opaque = 0,

    AlphaTest = 1000,

    Transparent = 2000,

    Overlay = 3000,
};

[[vk::binding(0, StapleUniformBufferSet)]]
public cbuffer StapleRenderData
{
    public float4x4 world;
    public float4x4 view;
    public float4x4 projection;
    public bool useWorldMatrix;
	public RenderQueue renderQueue;
    public float time;
#if defined(LIT) || (defined(SKINNING) && defined(STAPLE_VERTEX_SHADER))
	public float padding0;
#endif
#ifdef LIT
    float4 StapleLightCountViewPosition;
    float4 StapleLightAmbientColor;
    float4 StapleLightTypePosition[MaxStapleLightCount];
    float4 StapleLightDiffuse[MaxStapleLightCount];
#endif
#if defined(SKINNING) && defined(STAPLE_VERTEX_SHADER)
	public float StapleBlendShapeWeight0;
	public float StapleBlendShapeWeight1;
	public float StapleBlendShapeWeight2;
	public float StapleBlendShapeWeight3;
	public float StapleBlendShapeWeight4;
	public float StapleBlendShapeWeight5;
	public float StapleBlendShapeWeight6;
	public float StapleBlendShapeWeight7;
	public float StapleBlendShapeWeight8;
	public float StapleBlendShapeWeight9;
	public float StapleBlendShapeWeight10;
	public float StapleBlendShapeWeight11;
	public float StapleBlendShapeWeight12;
	public float StapleBlendShapeWeight13;
	public float StapleBlendShapeWeight14;
	public float StapleBlendShapeWeight15;
	public int StapleBlendShapeCount;
	public int StapleBlendShapeVertexCount;
#endif
};

#ifdef STAPLE_VERTEX_SHADER
public float4x4 StapleWorldMatrix(uint baseInstance, uint instanceID)
{
    if (useWorldMatrix)
    {
        return world;
    }

    uint transformIndex = StapleEntityTransformIDs[baseInstance + instanceID];

    return StapleEntityTransforms[transformIndex];
}

public float4x4 ProjectionViewWorld(float4x4 world)
{
    return mul(projection, mul(view, world));
}

public float4x4 ViewWorld(float4x4 world)
{
    return mul(view, world);
}
#endif

#if defined(SKINNING) && defined(STAPLE_VERTEX_SHADER)
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

public float StapleGetBlendWeight(int index)
{
	switch(index)
	{
		case 0:
		
			return StapleBlendShapeWeight0;

		case 1:
		
			return StapleBlendShapeWeight1;

		case 2:
		
			return StapleBlendShapeWeight2;

		case 3:
		
			return StapleBlendShapeWeight3;

		case 4:
		
			return StapleBlendShapeWeight4;

		case 5:
		
			return StapleBlendShapeWeight5;

		case 6:
		
			return StapleBlendShapeWeight6;

		case 7:
		
			return StapleBlendShapeWeight7;

		case 8:
		
			return StapleBlendShapeWeight8;

		case 9:
		
			return StapleBlendShapeWeight9;

		case 10:
		
			return StapleBlendShapeWeight10;

		case 11:
		
			return StapleBlendShapeWeight11;

		case 12:
		
			return StapleBlendShapeWeight12;

		case 13:
		
			return StapleBlendShapeWeight13;

		case 14:
		
			return StapleBlendShapeWeight14;

		case 15:
		
			return StapleBlendShapeWeight15;
	}
	
	return 0;
}

public float3 StapleGetBlendOffset(uint vertexID)
{
	if(StapleBlendShapeCount == 0)
	{
		return float3(0, 0, 0);
	}
	
	float3 outValue = float3(0, 0, 0);
	
	for(int i = 0; i < StapleBlendShapeCount; i++)
	{
		float weight = StapleGetBlendWeight(i);
		
		if(weight <= 0.0001)
		{
			continue;
		}
		
		uint index = i * StapleBlendShapeVertexCount * 2 + vertexID * 2;
		
		float3 offset = StapleBlendData[index].xyz * weight;
		
		outValue += offset;
	}
	
	return outValue;
}

#endif
