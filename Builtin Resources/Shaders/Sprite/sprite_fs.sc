$input v_texcoord0

#include <bgfx_shader.sh>

uniform vec4 u_color;

SAMPLER2D(s_texColor, 0);

void main()
{
    gl_FragColor = texture2D(s_texColor, v_texcoord0) * u_color;
}
