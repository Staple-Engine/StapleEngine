Type VertexFragment

Begin Parameters

color mainColor

End Parameters

Begin Input
POSITION
End Input

Begin Instancing
End Instancing

Begin Common

struct VertexOutput
{
    float4 position : SV_Position;
    float4 color;
};

End Common

Begin Vertex

[[vk::binding(StapleBufferIndexCount, StapleUniformBufferSet)]]
cbuffer Uniforms
{
    float4 mainColor;
};

struct Input
{
    float3 position : POSITION;
};

[shader("vertex")]
VertexOutput VertexMain(Input input)
{
    VertexOutput output;

    float3 position = input.position;
    float4 color = mainColor;

    output.color = color;
    output.position = mul(ProjectionViewWorld(world), float4(position, 1.0));

    return output;
}
End Vertex

Begin Fragment

[shader("fragment")]
float4 FragmentMain(VertexOutput input) : SV_Target
{
    return input.color;
}

End Fragment
