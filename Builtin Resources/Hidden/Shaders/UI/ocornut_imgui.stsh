Type VertexFragment
Blend SrcAlpha OneMinusSrcAlpha

Begin Parameters

varying vec2 v_texcoord0 : TEXCOORD0 = vec2(0.0, 0.0)
varying vec4 v_color0 : COLOR0 = vec4(0.0, 0.0, 0.0, 0.0)
varying vec3 a_position : POSITION
varying vec2 a_texcoord0 : TEXCOORD0
varying vec4 a_color0 : COLOR0

uniform texture s_tex

End Parameters

Begin Vertex

$input a_position, a_texcoord0, a_color0
$output v_color0, v_texcoord0

void main()
{
	vec4 pos = mul(u_viewProj, vec4(a_position.xy, 0.0, 1.0) );
	gl_Position = vec4(pos.x, pos.y, 0.0, 1.0);
	v_texcoord0 = a_texcoord0;
	v_color0    = a_color0;
}

End Vertex

Begin Fragment

$input v_color0, v_texcoord0

void main()
{
	vec4 texel = texture2D(s_tex, v_texcoord0);
	gl_FragColor = texel * v_color0;
}

End Fragment
