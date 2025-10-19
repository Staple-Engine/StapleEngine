Type VertexFragment
Blend SrcAlpha OneMinusSrcAlpha

Begin Parameters

uniform texture mainTexture

End Parameters

Begin Common

cbuffer Uniforms
{
	Sampler2D mainTexture;
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
	float4 color : COLOR0;
};

[shader("vertex")]
VertexOutput VertexMain(Input input)
{
    VertexOutput output;

    float3 position = input.position;

    output.color = input.color;
	output.coord = input.coord;
    output.position = mul(mul(mul(projection, view), world), float4(position, 1.0));

    return output;
}

End Vertex

Begin Fragment

[shader("fragment")]
float4 FragmentMain(VertexOutput input) : SV_Target
{
    return mainTexture.Sample(input.coord) * input.color;
}

End Fragment
