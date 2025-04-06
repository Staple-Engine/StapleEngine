#ifndef STAPLE_SHADER_GUARD
#define STAPLE_SHADER_GUARD

#include <bgfx_compute.sh>
#include <bgfx_shader.sh>

#define STAPLE_SKINNING_STAGE_INDEX 15
#define STAPLE_LIGHTING_NORMAL_MATRIX_STAGE_INDEX 14

#ifdef INSTANCING
#define StapleInstanceID gl_InstanceID
#else
#define StapleInstanceID 0.0
#endif

#ifdef INSTANCING
#define StapleModelMatrix mtxFromCols(i_data0, i_data1, i_data2, i_data3)
#else
#define StapleModelMatrix u_model[0]
#endif

#ifdef SKINNING
BUFFER_RO(StapleBoneMatrices, vec4, STAPLE_SKINNING_STAGE_INDEX);

mat4 StapleGetBoneMatrix(int index)
{
	return mtxFromCols(StapleBoneMatrices[index * 4],
		StapleBoneMatrices[index * 4 + 1],
		StapleBoneMatrices[index * 4 + 2],
		StapleBoneMatrices[index * 4 + 3]);
}

mat4 StapleGetSkinningMatrix(mat4 model, vec4 indices, vec4 weights)
{
	return mul(model, weights.x * StapleGetBoneMatrix(int(indices.x)) +
			weights.y * StapleGetBoneMatrix(int(indices.y)) +
			weights.z * StapleGetBoneMatrix(int(indices.z)) + 
			weights.w * StapleGetBoneMatrix(int(indices.w)));
}
#endif

#endif