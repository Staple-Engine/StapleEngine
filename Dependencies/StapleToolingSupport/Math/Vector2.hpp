#pragma once

#include "ufbx.h"

class Vector2
{
public:
	float x, y;

	Vector2() : x(0), y(0) {}

	Vector2(float x, float y) : x(x), y(y) {}

	Vector2(const ufbx_vec2 &v) : x(v.x), y(v.y) {}

	friend bool operator==(const Vector2& lhs, const Vector2& rhs)
	{
		return lhs.x == rhs.x && lhs.y == rhs.y;
	}

	friend bool operator!=(const Vector2& lhs, const Vector2& rhs)
	{
		return lhs.x != rhs.x || lhs.y != rhs.y;
	}

	static float Dot(const Vector2& a, const Vector2& b)
	{
		return a.x * b.x + a.y * b.y;
	}
};
