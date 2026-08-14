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

public uint StapleGetBlendShapeCount()
{
	return uint(StapleBlendShapeParameters[0].x);
}

public uint StapleGetBlendShapeVertexCount()
{
	return uint(StapleBlendShapeParameters[0].y);
}

public float StapleGetBlendWeight(uint index)
{
	return StapleBlendShapeParameters[index / 4 + 1][index % 4];
}

public float3 StapleGetBlendOffset(uint vertexID)
{
	float blendShapeCount = StapleGetBlendShapeCount();

	if(blendShapeCount == 0)
	{
		return float3(0, 0, 0);
	}

	uint vertexCount = StapleGetBlendShapeVertexCount();

	float3 outValue = float3(0, 0, 0);

	for(uint i = 0; i < blendShapeCount; i++)
	{
		float weight = StapleGetBlendWeight(i);

		if(weight <= 0.0001)
		{
			continue;
		}

		uint index = i * vertexCount * 2 + vertexID * 2;

		float3 offset = StapleBlendShapeData[index].xyz * weight;

		outValue += offset;
	}

	return outValue;
}

#endif
