implementing Staple;

#ifdef STAPLE_TEXTURE_COUNT
public static const int StapleTextureCount = STAPLE_TEXTURE_COUNT;
#else
public static const int StapleTextureCount = 0;
#endif

#ifdef STAPLE_STORAGE_TEXTURE_COUNT
public static const int StapleStorageTextureStart = StapleTextureCount;
public static const int StapleStorageTextureCount = STAPLE_STORAGE_TEXTURE_COUNT;
#else
public static const int StapleStorageTextureStart = 30;
public static const int StapleStorageTextureCount = 30;
#endif

#ifdef STAPLE_STORAGE_BUFFER_COUNT
public static const int StapleInternalStorageBufferStart = StapleStorageTextureStart + StapleStorageTextureCount;
public static const int StapleInternalStorageBufferCount = StapleInternalStorageBufferStart + STAPLE_STORAGE_BUFFER_COUNT;
#else
public static const int StapleInternalStorageBufferStart = 60;
public static const int StapleInternalStorageBufferCount = 60;
#endif

#ifdef STAPLE_UNIFORM_BUFFER_COUNT
public static const int StapleUniformBufferCount = STAPLE_UNIFORM_BUFFER_COUNT;
#else
public static const int StapleUniformBufferCount = 0;
#endif

#ifdef STAPLE_VERTEX_SHADER
public static const int StapleSamplerStorageBufferSet = 0;
public static const int StapleUniformBufferSet = 1;
#elif defined(STAPLE_FRAGMENT_SHADER)
public static const int StapleSamplerStorageBufferSet = 2;
public static const int StapleUniformBufferSet = 3;
#elif defined(STAPLE_COMPUTE_SHADER)
public static const int StapleSamplerStorageBufferSet = 0;
public static const int StapleReadWriteBufferSet = 1;
public static const int StapleUniformBufferSet = 2;
#else
public static const int StapleSamplerStorageBufferSet = 0;
public static const int StapleReadWriteBufferSet = 0;
public static const int StapleUniformBufferSet = 0;
#endif

#ifdef STAPLE_VERTEX_SHADER

	#ifdef LIT
		#ifdef SKINNING

			#define STAPLE_INTERNAL_STORAGE_BUFFER_COUNT 6
			#define SKINNING_BUFFER_INDEX 3
			#define SKINNING_BLEND_SHAPE_DATA_BUFFER_INDEX 4
			#define SKINNING_BLEND_SHAPE_PARAMETER_BUFFER_INDEX 5

		#else

			#define STAPLE_INTERNAL_STORAGE_BUFFER_COUNT 3

		#endif
	#else
		#ifdef SKINNING

			#define STAPLE_INTERNAL_STORAGE_BUFFER_COUNT 5
			#define SKINNING_BUFFER_INDEX 2
			#define SKINNING_BLEND_SHAPE_DATA_BUFFER_INDEX 3
			#define SKINNING_BLEND_SHAPE_PARAMETER_BUFFER_INDEX 4

		#else

			#define STAPLE_INTERNAL_STORAGE_BUFFER_COUNT 2

		#endif
	#endif

#else

	#ifdef LIT

		#define STAPLE_INTERNAL_STORAGE_BUFFER_COUNT 1

	#else

		#define STAPLE_INTERNAL_STORAGE_BUFFER_COUNT 0

	#endif

#endif

public static const int StapleStorageBufferStart = StapleInternalStorageBufferStart + STAPLE_INTERNAL_STORAGE_BUFFER_COUNT;
public static const int StapleUniformBufferStart = 1;

#ifdef STAPLE_VERTEX_SHADER

[[vk::binding(StapleInternalStorageBufferStart, StapleSamplerStorageBufferSet)]]
public StructuredBuffer<float4x4> StapleEntityTransforms;

[[vk::binding(StapleInternalStorageBufferStart + 1, StapleSamplerStorageBufferSet)]]
public StructuredBuffer<uint> StapleEntityTransformIDs;

	#ifdef LIT

[[vk::binding(StapleInternalStorageBufferStart + 2, StapleSamplerStorageBufferSet)]]
public StructuredBuffer<float4> StapleLightData;

	#endif

	#ifdef SKINNING

[[vk::binding(StapleInternalStorageBufferStart + SKINNING_BUFFER_INDEX, StapleSamplerStorageBufferSet)]]
public StructuredBuffer<float4> StapleBoneMatrices;

[[vk::binding(StapleInternalStorageBufferStart + SKINNING_BLEND_SHAPE_DATA_BUFFER_INDEX, StapleSamplerStorageBufferSet)]]
public StructuredBuffer<float4> StapleBlendShapeData;

[[vk::binding(StapleInternalStorageBufferStart + SKINNING_BLEND_SHAPE_PARAMETER_BUFFER_INDEX, StapleSamplerStorageBufferSet)]]
public StructuredBuffer<float4> StapleBlendShapeParameters;

	#endif
#else

#ifdef LIT

[[vk::binding(StapleInternalStorageBufferStart, StapleSamplerStorageBufferSet)]]
public StructuredBuffer<float4> StapleLightData;

#endif

#endif
