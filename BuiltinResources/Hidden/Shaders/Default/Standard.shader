Type VertexFragment

Blend SrcAlpha OneMinusSrcAlpha

Variants VERTEX_COLORS, LIT, HALF_LAMBERT, PER_VERTEX_LIGHTING, NORMALMAP, CUTOUT

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
variant: CUTOUT float cutout

End Parameters

Begin Input
POSITION
TEXCOORD0
NORMAL
variant: NORMALMAP TANGENT
variant: NORMALMAP BITANGENT
variant: VERTEX_COLORS|PER_VERTEX_LIGHTING COLOR0
variant: SKINNING BLENDINDICES
variant: SKINNING BLENDWEIGHTS
End Input

Begin Instancing
End Instancing

Begin Common

[[vk::binding(StapleUniformBufferStart, StapleUniformBufferSet)]]
cbuffer Uniforms
{
	float4 diffuseColor;
	float4 emissiveColor;
	float4 specularColor;
	float cutout;
	float alphaThreshold;
};

struct VertexOutput
{
	float4 position : SV_Position;
	float3 worldPosition;
#ifdef LIT
	float3 lightNormal;
#endif
	float2 coords;
	float3 normal;
#ifdef NORMALMAP
	float3 tangent;
	float3 bitangent;
#endif
#if defined(VERTEX_COLORS) || defined(PER_VERTEX_LIGHTING)
	float4 color;
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
#if defined(VERTEX_COLORS) || defined(PER_VERTEX_LIGHTING)
	float4 color : COLOR0;
#endif
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
	float4x4 viewWorld = ViewWorld(model);

	float4 vertexPosition = mul(projectionViewWorld, float4(input.position, 1.0));

	output.position = vertexPosition;

	output.worldPosition = mul(model, float4(input.position, 1.0)).xyz;

	output.coords = input.coords;
	output.normal = input.normal;

#ifdef NORMALMAP
	output.tangent = input.tangent;
	output.bitangent = input.bitangent;
#endif

#ifdef LIT
	output.lightNormal = StapleLightNormal(input.normal, model);
#endif

	output.instanceID = instanceID;
	
#if defined(LIT) && defined(PER_VERTEX_LIGHTING)
	output.color = float4(diffuseColor.rgb * StapleProcessLights(output.worldPosition, output.lightNormal), diffuseColor.a);
	
	#ifdef VERTEX_COLORS
		output.color = input.color * output.color;
	#endif
#elif defined(VERTEX_COLORS) || defined(PER_VERTEX_LIGHTING)
	output.color = input.color;
#endif

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
#if defined(VERTEX_COLORS) || defined(PER_VERTEX_LIGHTING)
	float4 diffuse = input.color * diffuseColor;
#else
	float4 diffuse = diffuseTexture.Sample(input.coords) * diffuseColor;
#endif

#ifdef CUTOUT
	if(diffuse.a < alphaThreshold)
	{
		discard;
	}
#endif
	
#if defined(LIT) && defined(PER_VERTEX_LIGHTING)
	return diffuse;
#elif defined(LIT)

#ifdef NORMALMAP
	float3x3 tbn = float3x3(normalize(input.tangent), normalize(input.bitangent), normalize(input.normal));

	float3 normalMapNormal = normalize(normalTexture.Sample(input.coords).xyz * 2.0 - 1.0);

	float3 light = StapleProcessLightsTangent(input.worldPosition, normalMapNormal, tbn);
#else
	float3 light = StapleProcessLights(input.worldPosition, input.lightNormal);
#endif

	return float4(light, 1) * diffuse;
#else
	return diffuse;
#endif
}

End Fragment
