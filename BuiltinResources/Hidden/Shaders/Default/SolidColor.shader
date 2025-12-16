Type VertexFragment

Begin Parameters

color mainColor

End Parameters

Begin Input
POSITION
variant: SKINNING BLENDINDICES
variant: SKINNING BLENDWEIGHTS
End Input

Begin Instancing
End Instancing

Begin Common

struct VertexOutput
{
    float4 position : SV_Position;
    float4 color;
};

End Common

Begin Vertex

[[vk::binding(StapleUniformBufferStart, StapleUniformBufferSet)]]
cbuffer Uniforms
{
    float4 mainColor;
};

struct Input
{
    float3 position : POSITION;
#ifdef SKINNING
	float4 indices : BLENDINDICES;
	float4 weights : BLENDWEIGHTS;
#endif
};

[shader("vertex")]
VertexOutput VertexMain(Input input, uint instanceID : SV_InstanceID)
{
    VertexOutput output;

    float3 position = input.position;
    float4 color = mainColor;

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

    output.color = color;
    output.position = mul(projectionViewWorld, float4(position, 1.0));

    return output;
}
End Vertex

Begin Fragment

[shader("fragment")]
float4 FragmentMain(VertexOutput input) : SV_Target
{
    return input.color;
}

End Fragment
