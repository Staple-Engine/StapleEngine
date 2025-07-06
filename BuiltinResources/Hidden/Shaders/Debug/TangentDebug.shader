Variants BITANGENT_COLOR

Begin Parameters

varying vec3 v_tangent : TANGENT
varying vec3 v_bitangent : BITANGENT

variant: BITANGENT_COLOR uniform float tangentOrBitangent

End Parameters

Begin Instancing
End Instancing

Begin Vertex

$input a_position, a_tangent, a_bitangent
$output v_tangent, v_bitangent

void main()
{
	mat4 model = StapleModelMatrix;

	#ifdef SKINNING
	model = StapleGetSkinningMatrix(model, a_indices, a_weight);
	#endif

	mat4 projViewWorld = mul(mul(u_proj, u_view), model);
	vec4 v_pos = mul(projViewWorld, vec4(a_position, 1.0));

	v_tangent = a_tangent;
	v_bitangent = a_bitangent;

	gl_Position = v_pos;
}

End Vertex

Begin Fragment

$input v_tangent, v_bitangent

void main()
{
#if BITANGENT
	vec3 color = v_bitangent;
#else
	vec3 color = v_tangent;
#endif
	
	gl_FragColor = vec4(color * 0.5 + 0.5, 1);
}

End Fragment
