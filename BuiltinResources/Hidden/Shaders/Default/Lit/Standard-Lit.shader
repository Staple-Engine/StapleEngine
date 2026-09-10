Type VertexFragment

ShaderDefines LIT

Blend SrcAlpha OneMinusSrcAlpha

Variants NORMALMAP

Begin Parameters

texture ambientOcclusionTexture
texture diffuseTexture = WHITE
variant: NORMALMAP texture normalTexture
texture displacementTexture
texture emissiveTexture
texture heightTexture
texture specularTexture
color diffuseColor = #FFFFFFFF
color emissiveColor
color specularColor
float alphaThreshold = 0.25

End Parameters

Begin Instancing
End Instancing

Begin Common

[[vk::binding(StapleUniformBufferStart, StapleUniformBufferSet)]]
cbuffer Uniforms
{
	float4 diffuseColor;
	float4 emissiveColor;
	float4 specularColor;
	float alphaThreshold;
};

struct VertexOutput
{
	float4 position : SV_Position;
	float3 worldPosition;
	float3 worldNormal;
	float2 coords;
	float3 normal;
#ifdef NORMALMAP
	float3 tangent;
	float3 bitangent;
#endif

	uint instanceID;
};

End Common

Begin Vertex

struct Input
{
	float3 position : POSITION;
	float2 coords : TEXCOORD0;
	float3 normal : NORMAL;

#ifdef NORMALMAP
	float3 tangent : TANGENT;
	float3 bitangent : BITANGENT;
#endif

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
	output.normal = input.normal;

#ifdef NORMALMAP
	output.tangent = input.tangent;
	output.bitangent = input.bitangent;
#endif

	output.worldNormal = StapleLightNormal(input.normal, model);

	output.instanceID = input.instanceID;

	return output;
}
End Vertex

Begin Fragment

[[vk::binding(0, StapleSamplerStorageBufferSet)]]
cbuffer Textures
{
	Sampler2D ambientOcclusionTexture;
	Sampler2D diffuseTexture;
	Sampler2D normalTexture;
	Sampler2D displacementTexture;
	Sampler2D emissiveTexture;
	Sampler2D heightTexture;
	Sampler2D specularTexture;
};

[shader("fragment")]
float4 FragmentMain(VertexOutput input) : SV_Target
{
	float4 diffuse = diffuseTexture.Sample(input.coords) * diffuseColor;

	if(renderQueue != RenderQueue::Opaque && diffuse.a < alphaThreshold)
	{
		discard;
	}

#ifdef NORMALMAP
	float3 normalMapNormal = StapleGetTangentNormal(input.worldNormal, input.tangent, input.bitangent, input.coords, normalTexture);

	float3 light = StapleProcessLights(input.worldPosition, normalMapNormal);
#else
	float3 light = StapleProcessLights(input.worldPosition, normalize(input.worldNormal));
#endif
 
	return float4(light * diffuse.rgb, diffuse.a);
}

End Fragment
