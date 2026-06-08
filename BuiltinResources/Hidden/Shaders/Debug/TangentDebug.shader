Variants BITANGENT_COLOR

Begin Parameters

variant: BITANGENT_COLOR float tangentOrBitangent

End Parameters

Begin Input
POSITION
TANGENT
variant: SKINNING BLENDINDICES
variant: SKINNING BLENDWEIGHTS
End Input

Begin Instancing
End Instancing

Begin Common

struct VertexOutput
{
    float4 position : SV_Position;
    float3 tangent;
};

End Common

Begin Vertex

struct Input
{
    float3 position : POSITION;

#ifdef BITANGENT
	float3 tangent : BITANGENT;
#else
	float3 tangent : TANGENT;
#endif

#ifdef SKINNING
	float4 indices : BLENDINDICES;
	float4 weights : BLENDWEIGHTS;
#endif

    uint baseInstance : SV_StartInstanceLocation;
    uint instanceID : SV_InstanceID;
};

[shader("vertex")]
VertexOutput VertexMain(Input input)
{
    VertexOutput output;

	float4x4 model = StapleWorldMatrix(input.baseInstance, input.instanceID);

#ifdef SKINNING
	model = StapleGetSkinningMatrix(model, input.indices, input.weights);
#endif

	float4x4 projectionViewWorld = ProjectionViewWorld(model);

    float3 position = input.position;

    output.tangent = input.tangent;
    output.position = mul(projectionViewWorld, float4(position, 1.0));

    return output;
}

End Vertex

Begin Fragment

[shader("fragment")]
float4 FragmentMain(VertexOutput input) : SV_Target
{
    return float4(normalize(input.tangent) * 0.5 + 0.5, 1);
}

End Fragment
