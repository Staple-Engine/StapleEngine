Begin Parameters

End Parameters

Begin Input
POSITION
NORMAL
End Input

Begin Instancing
End Instancing

Begin Common

struct VertexOutput
{
    float4 position : SV_Position;
    float3 normal;
};

End Common

Begin Vertex

struct Input
{
    float3 position : POSITION;
	float3 normal : NORMAL;
};

[shader("vertex")]
VertexOutput VertexMain(Input input)
{
    VertexOutput output;

    float3 position = input.position;

    output.normal = input.normal;
    output.position = mul(ProjectionViewWorld(world), float4(position, 1.0));

    return output;
}

End Vertex

Begin Fragment

[shader("fragment")]
float4 FragmentMain(VertexOutput input) : SV_Target
{
    return float4(normalize(input.normal) * 0.5 + 0.5, 1);
}

End Fragment
