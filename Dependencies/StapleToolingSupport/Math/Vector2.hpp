#pragma once

#include "ufbx.h"

class Vector2
{
public:
	float x, y;

	Vector2() : x(0), y(0) {}

	Vector2(float x, float y) : x(x), y(y) {}

	Vector2(const ufbx_vec2 &v) : x(v.x), y(v.y) {}
};
