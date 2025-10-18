Type VertexFragment

Begin Parameters

uniform color mainColor

End Parameters

Begin Instancing
End Instancing

Begin Vertex

struct Input
{
	float3 Position : TEXCOORD0;
};

struct Output
{
	float4 Position : SV_Position;
}

Output main(Input input)
{
	Output output;
	
	float4x4 model = StapleModelMatrix;

	float4x4 projViewWorld = mul(mul(u_proj, u_view), model);

	float4 v_pos = mul(projViewWorld, float4(a_position, 1.0));

	output.position = v_pos;
}

End Vertex

Begin Fragment

float4 main() : SV_Target0
{
	return mainColor;
}

End Fragment
