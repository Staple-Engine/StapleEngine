Type VertexFragment

Begin Parameters

varying vec3 a_position : POSITION
uniform color mainColor

End Parameters

Begin Vertex
$input a_position

void main()
{
	mat4 projViewWorld = mul(mul(u_proj, u_view), u_model[0]);

	vec4 v_pos = mul(projViewWorld, vec4(a_position, 1.0));

	gl_Position = v_pos;
}

End Vertex

Begin Fragment

void main()
{
	gl_FragColor = mainColor;
}

End Fragment
