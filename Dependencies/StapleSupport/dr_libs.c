#define DR_MP3_IMPLEMENTATION
#include "../dr_libs/Original/dr_mp3.h"
#include "common.h"

void Free(void *ptr, void *userdata)
{
	free(ptr);
}

void *Malloc(size_t size, void *userdata)
{
	return malloc(size);
}

void *Realloc(void *ptr, size_t size, void *userdata)
{
	return realloc(ptr, size);
}

drmp3_allocation_callbacks callbacks = {
	.onFree = Free,
	.onMalloc = Malloc,
	.onRealloc = Realloc,
	.pUserData = NULL,
};

typedef struct
{
	short* buffer;
}MP3Data;

EXPORT void* DrLibsLoadMP3(void* ptr, int length, int* channels, int *bitsPerChannel, int* sampleRate, float *duration, int* requiredSize)
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

EXPORT short* DrLibsGetMP3Buffer(void* ptr)
{
	MP3Data* data = (MP3Data*)ptr;

	return data->buffer;
}

EXPORT void DrLibsFreeMP3(void* ptr)
{
	MP3Data* data = (MP3Data*)ptr;

	drmp3_free(data->buffer, &callbacks);

	free(data);
}
