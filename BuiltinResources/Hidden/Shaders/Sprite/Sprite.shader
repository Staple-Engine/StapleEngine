Type VertexFragment

Blend SrcAlpha OneMinusSrcAlpha

Begin Parameters

color mainColor
texture mainTexture

End Parameters

Begin Input
POSITION
TEXCOORD0
End Input

Begin Common

[[vk::binding(StapleBufferIndexCount, StapleUniformBufferSet)]]
cbuffer Uniforms
{
	float4 mainColor;
};

struct VertexOutput
{
    float4 position : SV_Position;
	float2 coord;
    float4 color;
};

End Common

Begin Vertex

struct Input
{
    float3 position : POSITION;
	float2 coord : TEXCOORD0;
};

[shader("vertex")]
VertexOutput VertexMain(Input input)
{
    VertexOutput output;

    float3 position = input.position;
    float4 color = mainColor;

    output.color = color;
	output.coord = input.coord;
    output.position = mul(ProjectionViewWorld(world), float4(position, 1.0));

    return output;
}

End Vertex

Begin Fragment

[[vk::binding(0, StapleSamplerBufferSet)]]
cbuffer Textures
{
	Sampler2D mainTexture;
};

[shader("fragment")]
float4 FragmentMain(VertexOutput input) : SV_Target
{
    return mainTexture.Sample(input.coord) * input.color;
}

End Fragment
