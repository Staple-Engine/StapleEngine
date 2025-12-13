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

#if defined(SKINNING) && defined(LIT)
#define STAPLE_INTERNAL_STORAGE_BUFFER_COUNT 2
#define STAPLE_INTERNAL_UNIFORM_BUFFER_START 3
#elif defined(SKINNING) || defined(LIT)
#define STAPLE_INTERNAL_STORAGE_BUFFER_COUNT 1
#ifdef LIT
#define STAPLE_INTERNAL_UNIFORM_BUFFER_START 3
#else
#define STAPLE_INTERNAL_UNIFORM_BUFFER_START 2
#endif
#else
#define STAPLE_INTERNAL_STORAGE_BUFFER_COUNT 0
#define STAPLE_INTERNAL_UNIFORM_BUFFER_START 2
#endif

public static const int StapleStorageBufferStart = StapleInternalStorageBufferStart + STAPLE_INTERNAL_STORAGE_BUFFER_COUNT;
public static const int StapleUniformBufferStart = STAPLE_INTERNAL_UNIFORM_BUFFER_START;

#if defined(SKINNING) && defined(LIT)

[[vk::binding(StapleInternalStorageBufferStart, StapleSamplerStorageBufferSet)]]
public StructuredBuffer<float4> StapleBoneMatrices;

[[vk::binding(StapleInternalStorageBufferStart + 1, StapleSamplerStorageBufferSet)]]
public StructuredBuffer<Light> StapleLights;

#else
#ifdef SKINNING

[[vk::binding(StapleInternalStorageBufferStart, StapleSamplerStorageBufferSet)]]
public StructuredBuffer<float4> StapleBoneMatrices;

#elif defined(LIT)

[[vk::binding(StapleInternalStorageBufferStart, StapleSamplerStorageBufferSet)]]
public StructuredBuffer<Light> StapleLights;

#endif
#endif
