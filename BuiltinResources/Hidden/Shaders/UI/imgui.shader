Type VertexFragment
Blend SrcAlpha OneMinusSrcAlpha

Begin Parameters

texture mainTexture

End Parameters

Begin Input
POSITION
TEXCOORD0
COLOR0
End Input

Begin Common

struct VertexOutput
{
    float4 position : SV_Position;
	float2 coord : TEXCOORD0;
    float4 color : COLOR0;
};

End Common

Begin Vertex

struct Input
{
    float3 position : POSITION;
	float2 coord : TEXCOORD0;
	uint color : COLOR0;
};

[shader("vertex")]
VertexOutput VertexMain(Input input)
{
    VertexOutput output;

    float3 position = input.position;

    output.color = float4(
        (input.color & 0xFF) / 255.0,
        (input.color >> 8 & 0xFF) / 255.0,
        (input.color >> 16 & 0xFF) / 255.0,
        (input.color >> 24 & 0xFF) / 255.0
    );

	output.coord = input.coord;
    output.position = mul(ProjectionViewWorld(world), float4(position, 1.0));

    return output;
}

End Vertex

Begin Fragment

[[vk::binding(0, StapleSamplerStorageBufferSet)]]
cbuffer Uniforms
{
	Sampler2D mainTexture;
};

[shader("fragment")]
float4 FragmentMain(VertexOutput input) : SV_Target
{
    return mainTexture.Sample(input.coord) * input.color;
}

End Fragment
