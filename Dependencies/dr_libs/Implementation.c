#define DR_MP3_IMPLEMENTATION
#include "Original/dr_mp3.h"

#ifdef _WIN32
#define EXPORT __declspec(dllexport)
#else
#define EXPORT
#endif

drmp3_allocation_callbacks callbacks = {
	.onFree = free,
	.onMalloc = malloc,
	.onRealloc = realloc,
	.pUserData = NULL,
};

typedef struct
{
	short* buffer;
}MP3Data;

EXPORT void* LoadMP3(void* ptr, int length, int* channels, int *bitsPerChannel, int* sampleRate, float *duration, int* requiredSize)
{
	drmp3_config config;

	drmp3_uint64 frameCount;

	short* buffer = drmp3_open_memory_and_read_pcm_frames_s16(ptr, length, &config, &frameCount, &callbacks);

	if (buffer == NULL)
	{
		return NULL;
	}

	*channels = config.channels;
	*bitsPerChannel = 16;
	*sampleRate = config.sampleRate;
	*duration = frameCount / (float)config.sampleRate;
	*requiredSize = sizeof(short) * frameCount;

	MP3Data* data = (MP3Data*)malloc(sizeof(MP3Data));

	if (data == NULL)
	{
		drmp3_free(buffer, &callbacks);

		free(data);

		return NULL;
	}

	data->buffer = buffer;

	return data;
}

EXPORT short* GetMP3Buffer(void* ptr)
{
	MP3Data* data = (MP3Data*)ptr;

	return data->buffer;
}

EXPORT void FreeMP3(void* ptr)
{
	MP3Data* data = (MP3Data*)ptr;

	drmp3_free(data->buffer, &callbacks);

	free(data);
}
