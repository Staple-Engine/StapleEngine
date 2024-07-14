Type VertexFragment

Blend SrcAlpha OneMinusSrcAlpha

Begin Parameters

varying vec2 v_texcoord0 : TEXCOORD0 = vec2(0.0, 0.0)
varying vec3 a_position : POSITION
varying vec2 a_texcoord0 : TEXCOORD0

uniform color mainColor
uniform texture mainTexture

End Parameters

Begin Vertex

$input a_position, a_texcoord0
$output v_texcoord0

void main()
{
	mat4 projViewWorld = mul(mul(u_proj, u_view), u_model[0]);

	vec4 v_pos = mul(projViewWorld, vec4(a_position, 1.0));

	gl_Position = v_pos;

	v_texcoord0 = a_texcoord0;
}

End Vertex

Begin Fragment

$input v_texcoord0

void main()
{
	gl_FragColor = texture2D(mainTexture, v_texcoord0) * mainColor;
}

End Fragment
