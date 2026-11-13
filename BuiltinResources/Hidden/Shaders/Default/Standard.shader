Type VertexFragment

Variants TRANSPARENT

variant: TRANSPARENT Blend SrcAlpha OneMinusSrcAlpha

Begin Parameters

texture diffuseTexture = WHITE
color diffuseColor = #FFFFFFFF
float alphaThreshold = 0.25

End Parameters

Begin Instancing
End Instancing

Begin Common

[[vk::binding(StapleUniformBufferStart, StapleUniformBufferSet)]]
cbuffer Uniforms
{
	float4 diffuseColor;
	float alphaThreshold;
};

struct VertexOutput
{
	float4 position : SV_Position;
	float3 worldPosition;
	float2 coords;

	uint instanceID;
};

End Common

Begin Vertex

struct Input
{
	float3 position : POSITION;
	float2 coords : TEXCOORD0;

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
	float4x4 viewWorld = ViewWorld(model);

	float4 vertexPosition = mul(projectionViewWorld, float4(position, 1.0));

	output.position = vertexPosition;

	output.worldPosition = mul(model, float4(position, 1.0)).xyz;

	output.coords = input.coords;

	output.instanceID = input.instanceID;

	return output;
}
End Vertex

Begin Fragment

[[vk::binding(0, StapleSamplerStorageBufferSet)]]
cbuffer Textures
{
	Sampler2D diffuseTexture;
};

[shader("fragment")]
float4 FragmentMain(VertexOutput input) : SV_Target
{
	float4 diffuse = diffuseTexture.Sample(input.coords) * diffuseColor;

	if(renderQueue != RenderQueue::Opaque && diffuse.a < alphaThreshold)
	{
		discard;
	}

	return diffuse;
}

End Fragment
