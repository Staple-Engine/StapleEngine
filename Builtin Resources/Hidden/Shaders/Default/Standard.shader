Type VertexFragment

Variants VERTEX_COLORS, LIT, HALF_LAMBERT, PER_VERTEX_LIGHTING

Begin Parameters

varying vec2 v_texcoord0 : TEXCOORD0 = vec2(0.0, 0.0)
varying vec3 a_position : POSITION
varying vec3 a_normal : NORMAL
varying vec2 a_texcoord0 : TEXCOORD0
varying vec4 a_weight : BLENDWEIGHT
varying vec4 a_indices : BLENDINDICES
varying vec3 v_fragPos : TEXCOORD1
varying vec3 v_normal : NORMAL
varying vec4 a_color0 : COLOR
varying vec4 v_color : COLOR

uniform texture ambientOcclusionTexture
uniform color diffuseColor
uniform texture diffuseTexture
uniform texture displacementTexture
uniform color emissiveColor
uniform texture emissiveTexture
uniform texture heightTexture
uniform texture normalTexture
uniform color specularColor
uniform texture specularTexture

End Parameters

Begin Vertex

$input a_position, a_texcoord0, a_normal, a_color0
$output v_texcoord0, v_fragPos, v_normal, v_color

#include "StapleLighting.sh"

void main()
{
	mat4 model = u_model[0];

	#ifdef SKINNING
	model = StapleGetSkinningMatrix(a_indices, a_weight);
	#endif

	mat4 projViewWorld = mul(mul(u_proj, u_view), model);
	mat4 viewWorld = mul(u_view, model);

	vec4 v_pos = mul(projViewWorld, vec4(a_position, 1.0));

	gl_Position = v_pos;

	v_texcoord0 = a_texcoord0;
	v_fragPos = mul(viewWorld, vec4(a_position, 1.0)).xyz;
	v_normal = a_normal;
	
#if LIT && PER_VERTEX_LIGHTING
	v_color = vec4(diffuseColor.rgb * StapleProcessLights(u_viewPos, v_fragPos, v_normal), diffuseColor.a);
	
	#if VERTEX_COLORS
		v_color = a_color0 * v_color;
	#endif
#else
	v_color = a_color0;
#endif
}
End Vertex

Begin Fragment

$input v_texcoord0, v_fragPos, v_normal, v_color

#include "StapleLighting.sh"

void main()
{
	#if VERTEX_COLORS || PER_VERTEX_LIGHTING
	vec4 diffuse = v_color * diffuseColor;
	#else
	vec4 diffuse = texture2D(diffuseTexture, v_texcoord0) * diffuseColor;
	#endif
	
#if LIT && PER_VERTEX_LIGHTING
	gl_FragColor = diffuse;
#elif LIT
	vec3 light = StapleProcessLights(u_viewPos, v_fragPos, v_normal);

	gl_FragColor = vec4(light, 1) * diffuse;
#else
	gl_FragColor = diffuse;
#endif
}

End Fragment