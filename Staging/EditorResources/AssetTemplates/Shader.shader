Type VertexFragment

Begin Parameters
color diffuse = #FFFFFFFF
End Parameters

Begin Common

[[vk::binding(StapleUniformBufferStart, StapleUniformBufferSet)]]
cbuffer Uniforms
{
	float4 diffuse;
};

struct VertexOutput
{
	float3 position : SV_Position;

	uint instanceID;
};

End Common

Begin Vertex

struct Input
{
	float3 position : POSITION;

#ifdef SKINNING
	float4 indices : BLENDINDICES;
	float4 weights : BLENDWEIGHTS;
#endif

    uint baseInstance : SV_StartInstanceLocation;
    uint instanceID : SV_InstanceID;
	uint baseVertex : SV_StartVertexLocation;
	uint vertexID : SV_VertexID;
};

[shader("vertex")]
VertexOutput VertexMain(Input input)
{
	VertexOutput output;

	float4x4 model = StapleWorldMatrix(input.baseInstance, input.instanceID);
	float3 position = input.position;

#ifdef SKINNING
	model = StapleGetSkinningMatrix(model, input.indices, input.weights);
	position += StapleGetBlendOffset(input.baseVertex + input.vertexID);
#endif

	float4x4 projectionViewWorld = ProjectionViewWorld(model);

	float3 vertexPosition = mul(projectionViewWorld, float4(position, 1.0)).xyz;

	output.position = vertexPosition;

	output.instanceID = input.instanceID;

	return output;
}
End Vertex

Begin Fragment

[shader("fragment")]
float4 FragmentMain(VertexOutput input) : SV_Target
{
	return diffuse;
}

End Fragment
