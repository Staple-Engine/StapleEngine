Begin Parameters

End Parameters

Begin Input
POSITION
NORMAL
variant: SKINNING BLENDINDICES
variant: SKINNING BLENDWEIGHTS
End Input

Begin Instancing
End Instancing

Begin Common

struct VertexOutput
{
    float4 position : SV_Position;
    float3 normal;
};

End Common

Begin Vertex

struct Input
{
    float3 position : POSITION;
	float3 normal : NORMAL;
#ifdef SKINNING
	float4 indices : BLENDINDICES;
	float4 weights : BLENDWEIGHTS;
#endif
};

[shader("vertex")]
VertexOutput VertexMain(Input input, uint instanceID : SV_InstanceID)
{
    VertexOutput output;

	float4x4 model;

#ifdef INSTANCING
	model = StapleGetInstancedTransform(instanceID);
#else
	model = world;
#endif

#ifdef SKINNING
	model = StapleGetSkinningMatrix(model, input.indices, input.weights);
#endif

	float4x4 projectionViewWorld = ProjectionViewWorld(model);

    float3 position = input.position;

    output.normal = input.normal;
    output.position = mul(projectionViewWorld, float4(position, 1.0));

    return output;
}

End Vertex

Begin Fragment

[shader("fragment")]
float4 FragmentMain(VertexOutput input) : SV_Target
{
    return float4(normalize(input.normal) * 0.5 + 0.5, 1);
}

End Fragment
