Type VertexFragment

ZWrite Off

Begin Parameters

texture sky

End Parameters

Begin Common

struct VertexOutput
{
	float4 position : SV_Position;
	float3 viewDirection;
};

End Common

Begin Vertex

struct Input
{
	float3 position : POSITION;

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
	
	output.viewDirection = normalize(position);
	
	float4x4 viewNoTranslate = view;
	
	viewNoTranslate[0][3] = 0;
	viewNoTranslate[1][3] = 0;
	viewNoTranslate[2][3] = 0;
	
	float4x4 projectionView = mul(projection, viewNoTranslate);

	output.position = mul(projectionView, float4(position, 1.0));

	return output;
}
End Vertex

Begin Fragment

[[vk::binding(0, StapleSamplerStorageBufferSet)]]
cbuffer Textures
{
	Sampler2D sky;
};

[shader("fragment")]
float4 FragmentMain(VertexOutput input) : SV_Target
{
	float3 direction = normalize(input.viewDirection);
	float u = atan2(direction.z, direction.x) / (2.0 * 3.14159265358979) + 0.5;
	float v = acos(direction.y) / 3.14159265358979;
	
	return sky.Sample(float2(u, v));
}

End Fragment
