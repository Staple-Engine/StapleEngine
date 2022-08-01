$input a_position, a_color0, a_texcoord0
$output v_color0, v_texcoord0
#include <bgfx_shader.sh>

void main()
{
    mat4 projViewWorld = mul(mul(u_proj, u_view), u_model[0]);

    vec4 v_pos = mul(projViewWorld, vec4(a_position, 1.0));

    gl_Position = v_pos;

    v_color0 = a_color0;
	v_texcoord0 = a_texcoord0;
}
