Type VertexFragment

Blend SrcAlpha OneMinusSrcAlpha

Variants VERTEX_COLORS, LIT, HALF_LAMBERT, PER_VERTEX_LIGHTING, NORMALMAP, CUTOUT

Begin Parameters

uniform vec3 viewPosition;
uniform texture ambientOcclusionTexture
uniform color diffuseColor = #FFFFFFFF
uniform texture diffuseTexture = WHITE
uniform texture displacementTexture
uniform color emissiveColor
uniform texture emissiveTexture
uniform texture heightTexture
variant: NORMALMAP uniform texture normalTexture
uniform color specularColor
uniform texture specularTexture
variant: CUTOUT uniform float cutout
uniform float alphaThreshold = 0.25

End Parameters

Begin Input
POSITION
TEXCOORD0
NORMAL
TANGENT
BITANGENT
COLOR0
End Input

Begin Instancing
End Instancing

Begin Common

/*
[[vk::binding(StapleBufferIndexCount, StapleUniformBufferSet)]]
cbuffer Uniforms
{
	float3 viewPosition;
	float4 diffuseColor;
	float4 emissiveColor;
	float4 specularColor;
	float cutout;
	float alphaThreshold;
};
*/

struct VertexOutput
{
	float4 position : SV_Position;
	float3 worldPosition;
	float3 lightNormal;
	float2 coords;
	float3 normal;
	float3 tangent;
	float3 bitangent;
	float4 color;
	uint instanceID;
};

End Common

Begin Vertex

struct Input
{
	float3 position : POSITION;
	float2 coords : TEXCOORD0;
	float3 normal : NORMAL;
	float3 tangent : TANGENT;
	float3 bitangent : BITANGENT;
	float4 color : COLOR0;
	uint instanceID : SV_InstanceID;
};

[shader("vertex")]
VertexOutput VertexMain(Input input)
{
	VertexOutput output;

	float4x4 model = world;

	float4x4 projectionViewWorld = ProjectionViewWorld(model);
	float4x4 viewWorld = ViewWorld(model);

	float4 vertexPosition = mul(projectionViewWorld, float4(input.position, 1.0));

	output.position = vertexPosition;

	output.worldPosition = mul(model, float4(input.position, 1.0)).xyz;

	output.coords = input.coords;
	output.normal = input.normal;
	output.tangent = input.tangent;
	output.bitangent = input.bitangent;
	output.lightNormal = StapleLightNormal(input.normal, model);
	output.instanceID = input.instanceID;
	
//TODO: handle light array
/*
#if defined(LIT) && defined(PER_VERTEX_LIGHTING)
	output.color = float4x4(diffuseColor.rgb * StapleProcessLights(viewPosition, output.worldPosition, input.normal), diffuseColor.a);
	
	#ifdef VERTEX_COLORS
		output.color = input.color * output.color;
	#endif
#else
	output.color = input.color;
#endif
*/
	output.color = input.color;

	return output;
}
End Vertex

Begin Fragment

[[vk::binding(0, StapleSamplerBufferSet)]]
cbuffer Textures
{
	Sampler2D diffuseTexture;
#ifdef NORMALMAP
	Sampler2D normalTexture;
#endif
};

[shader("fragment")]
float4 FragmentMain(VertexOutput input) : SV_Target
{
#if defined(VERTEX_COLORS) || defined(PER_VERTEX_LIGHTING)
	float4 diffuse = input.color;// * diffuseColor;
#else
	float4 diffuse = diffuseTexture.Sample(input.coords);// * diffuseColor;
#endif

/*	
#ifdef CUTOUT
	if(diffuse.a < alphaThreshold)
	{
		discard;
	}
#endif
*/
	
//TODO: handle light array
/*
#if defined(LIT) && defined(PER_VERTEX_LIGHTING)
	return diffuse;
#elif defined(LIT)

#ifdef NORMALMAP
	float3x3 tbn = float3x3(normalize(input.tangent), normalize(input.bitangent), normalize(input.normal));

	float3 normalMapNormal = normalize(normalTexture.Sample(v_texcoord0).xyz * 2.0 - 1.0);

	float3 light = StapleProcessLightsTangent(viewPosition, input.worldPosition, normalMapNormal, tbn);
#else
	float3 light = StapleProcessLights(viewPosition, input.worldPosition, input.lightNormal);
#endif

	return float4(light, 1) * diffuse;
#else
	return diffuse;
#endif
*/
	return diffuse;
}

End Fragment
