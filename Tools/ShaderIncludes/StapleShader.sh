#ifndef STAPLE_SHADER_GUARD
#define STAPLE_SHADER_GUARD

#include <bgfx_compute.sh>
#include <bgfx_shader.sh>

#define STAPLE_SKINNING_STAGE_INDEX 15

#ifdef SKINNING
BUFFER_RO(StapleBoneMatrices, vec4, STAPLE_SKINNING_STAGE_INDEX);

mat4 StapleGetBoneMatrix(int index)
{
	return mtxFromCols(StapleBoneMatrices[index * 4],
		StapleBoneMatrices[index * 4 + 1],
		StapleBoneMatrices[index * 4 + 2],
		StapleBoneMatrices[index * 4 + 3]);
}

mat4 StapleGetSkinningMatrix(vec4 indices, vec4 weights)
{
	return mul(u_model[0], weights.x * StapleGetBoneMatrix(int(indices.x)) +
			weights.y * StapleGetBoneMatrix(int(indices.y)) +
			weights.z * StapleGetBoneMatrix(int(indices.z)) + 
			weights.w * StapleGetBoneMatrix(int(indices.w)));
}
#endif

#endif