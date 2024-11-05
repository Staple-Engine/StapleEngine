#pragma once

#include <stdio.h>
#include <stdarg.h>
#include <string.h>

#ifdef _WIN32
#define EXPORT __declspec(dllexport)
#define CEXPORT extern "C" __declspec(dllexport)
#define IMPORT __declspec(dllimport)
#define CIMPORT extern "C" __declspec(dllimport)
#else
#define EXPORT
#define CEXPORT extern "C"
#define IMPORT
#define CIMPORT extern "C"
#endif

#ifdef __ANDROID__
#include <android/log.h>
#endif

#define CLAMP(value, min, max) (value < min ? min : value > max ? max : value)

inline void NativeLog(const char* fmt, ...)
{
	va_list args;

	va_start(args, fmt);

	char buffer[10240];

	memset(buffer, 0, sizeof(buffer));

	vsnprintf(buffer, sizeof(buffer) / sizeof(buffer[0]), fmt, args);

	va_end(args);

#ifdef __ANDROID__
	__android_log_print(ANDROID_LOG_DEBUG, "Staple Engine", "%s", buffer);
#else
	printf("%s", buffer);
#endif
}
