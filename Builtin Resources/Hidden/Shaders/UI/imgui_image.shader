Type VertexFragment
Blend SrcAlpha OneMinusSrcAlpha

Begin Parameters

varying vec2 v_texcoord0 : TEXCOORD0 = vec2(0.0, 0.0)
varying vec3 a_position : POSITION
varying vec2 a_texcoord0 : TEXCOORD0
varying vec4 a_color0 : COLOR0
varying vec4 v_color0 : COLOR0

uniform vec4 u_imageLodEnabled
uniform texture s_tex

End Parameters

Begin Vertex

$input a_position, a_texcoord0, a_color0
$output v_texcoord0, v_color0

/*
 * Copyright 2014 Dario Manesku. All rights reserved.
 * License: https://github.com/bkaradzic/bgfx/blob/master/LICENSE
 */

void main()
{
	gl_Position = mul(u_viewProj, vec4(a_position.xy, 0.0, 1.0) );
	v_texcoord0 = a_texcoord0;
	v_color0 = a_color0;
}

End Vertex

Begin Fragment

$input v_texcoord0, v_color0

/*
 * Copyright 2014 Dario Manesku. All rights reserved.
 * License: https://github.com/bkaradzic/bgfx/blob/master/LICENSE
 */

void main()
{
	vec4 color = texture2D(s_tex, v_texcoord0) * v_color0;
	gl_FragColor = color;
}

End Fragment
