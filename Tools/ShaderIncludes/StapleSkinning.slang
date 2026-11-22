implementing Staple;

__include StapleInternal;

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

public float3 StapleGetBlendNormalOffset(uint vertexID)
{
    float blendShapeCount = StapleGetBlendShapeCount();

    if (blendShapeCount == 0)
    {
        return float3(0, 0, 0);
    }

    uint vertexCount = StapleGetBlendShapeVertexCount();

    float3 outValue = float3(0, 0, 0);

    for (uint i = 0; i < blendShapeCount; i++)
    {
        float weight = StapleGetBlendWeight(i);

        if (weight <= 0.0001)
        {
            continue;
        }

        uint index = i * vertexCount * 2 + vertexID * 2 + 1;

        float3 offset = StapleBlendShapeData[index].xyz * weight;

        outValue += offset;
    }

    return outValue;
}

#endif
