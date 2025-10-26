Variants BITANGENT_COLOR

Begin Parameters

variant: BITANGENT_COLOR uniform float tangentOrBitangent

End Parameters

Begin Input
POSITION
TANGENT
End Input

Begin Instancing
End Instancing

Begin Common

struct VertexOutput
{
    float4 position : SV_Position;
    float3 tangent;
};

End Common

Begin Vertex

struct Input
{
    float3 position : POSITION;

#ifdef BITANGENT
	float3 tangent : BITANGENT;
#else
	float3 tangent : TANGENT;
#endif
};

[shader("vertex")]
VertexOutput VertexMain(Input input)
{
    VertexOutput output;

    float3 position = input.position;

    output.tangent = input.tangent;
    output.position = mul(ProjectionViewWorld(world), float4(position, 1.0));

    return output;
}

End Vertex

Begin Fragment

[shader("fragment")]
float4 FragmentMain(VertexOutput input) : SV_Target
{
    return float4(normalize(input.tangent) * 0.5 + 0.5, 1);
}

End Fragment
