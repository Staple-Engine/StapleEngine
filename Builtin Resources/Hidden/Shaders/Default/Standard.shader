Type VertexFragment

Variants VERTEX_COLORS, LIT, HALF_LAMBERT, PER_VERTEX_LIGHTING, NORMALMAP

Begin Parameters

varying vec2 v_texcoord0 : TEXCOORD0 = vec2(0.0, 0.0)
varying vec3 v_fragPos : TEXCOORD1
varying vec3 v_normal : NORMAL
varying vec4 v_color : COLOR
varying int v_instanceID : TEXCOORD2
varying vec3 v_tangent : TANGENT
varying vec3 v_bitangent : BITANGENT

uniform texture ambientOcclusionTexture
uniform color diffuseColor = #FFFFFFFF
uniform texture diffuseTexture = WHITE
uniform texture displacementTexture
uniform color emissiveColor
uniform texture emissiveTexture
uniform texture heightTexture
variant: NORMALMAP uniform texture normalTexture
uniform color specularColor
uniform texture specularTexture

End Parameters

Begin Instancing
End Instancing

Begin Vertex

$input a_position, a_texcoord0, a_normal, a_color0, a_tangent, a_bitangent
$output v_texcoord0, v_fragPos, v_normal, v_color, v_instanceID, v_tangent, v_bitangent

#include "StapleLighting.sh"

void main()
{
	mat4 model = StapleModelMatrix;

	#ifdef SKINNING
	model = StapleGetSkinningMatrix(model, a_indices, a_weight);
	#endif

	mat4 projViewWorld = mul(mul(u_proj, u_view), model);
	mat4 viewWorld = mul(u_view, model);

	vec4 v_pos = mul(projViewWorld, vec4(a_position, 1.0));

	gl_Position = v_pos;

	v_texcoord0 = a_texcoord0;
	v_fragPos = mul(viewWorld, vec4(a_position, 1.0)).xyz;
	v_normal = a_normal;
	
	v_tangent = a_tangent;
	v_bitangent = a_bitangent;
	
	v_instanceID = StapleInstanceID;
	
#if LIT && PER_VERTEX_LIGHTING
	v_color = vec4(diffuseColor.rgb * StapleProcessLights(int(v_instanceID), u_viewPos, v_fragPos, v_normal), diffuseColor.a);
	
	#if VERTEX_COLORS
		v_color = a_color0 * v_color;
	#endif
#else
	v_color = a_color0;
#endif
}
End Vertex

Begin Fragment

$input v_texcoord0, v_fragPos, v_normal, v_color, v_instanceID, v_tangent, v_bitangent

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

	#if NORMALMAP
	mat3 tbn = mtxFromCols(normalize(v_tangent), normalize(v_bitangent), normalize(v_normal));

	vec3 normalMapNormal = normalize(texture2D(normalTexture, v_texcoord0).xyz * 2.0 - 1.0);

	vec3 light = StapleProcessLightsTangent(int(v_instanceID), u_viewPos, v_fragPos, normalMapNormal, tbn);
	#else
	vec3 light = StapleProcessLights(int(v_instanceID), u_viewPos, v_fragPos, v_normal);
	#endif

	gl_FragColor = vec4(light, 1) * diffuse;
#else
	gl_FragColor = diffuse;
#endif
}

End Fragment
