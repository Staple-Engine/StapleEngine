#pragma once

#include "ufbx.h"

class Vector3
{
public:
	float x, y, z;

	Vector3() : x(0), y(0), z(0) {}

	Vector3(float x, float y, float z) : x(x), y(y), z(z) {}

	Vector3(const ufbx_vec3& v) : x(v.x), y(v.y), z(v.z) {}
};
