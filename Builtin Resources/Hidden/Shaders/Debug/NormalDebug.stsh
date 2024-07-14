Type VertexFragment

Begin Parameters

varying vec3 a_position : POSITION
varying vec3 a_normal : NORMAL
varying vec3 v_normal : NORMAL

End Parameters

Begin Vertex

$input a_position, a_normal
$output v_normal

void main()
{
	mat4 projViewWorld = mul(mul(u_proj, u_view), u_model[0]);
	vec4 v_pos = mul(projViewWorld, vec4(a_position, 1.0));

   v_normal = a_normal;

	gl_Position = v_pos;
}

End Vertex

Begin Fragment

$input v_normal

void main()
{
	gl_FragColor = vec4(v_normal * 0.5 + 0.5, 1);
}

End Fragment
