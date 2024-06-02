#pragma once

#ifdef _WIN32
#define EXPORT __declspec(dllexport)
#define CEXPORT extern "C" __declspec(dllexport)
#else
#define EXPORT
#define CEXPORT extern "C"
#endif

#define CLAMP(value, min, max) (value < min ? min : value > max ? max : value)
