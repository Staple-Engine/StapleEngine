#pragma once

#include "ufbx.h"

class Vector4
{
public:
	float x, y, z, w;

	Vector4() : x(0), y(0), z(0), w(0) {}

	Vector4(float x, float y, float z, float w) : x(x), y(y), z(z), w(w) {}

	Vector4(const ufbx_vec4 v) : x(v.x), y(v.y), z(v.z), w(v.w) {}

	Vector4(const ufbx_quat v) : x(v.x), y(v.y), z(v.z), w(v.w) {}

	friend bool operator==(const Vector4& lhs, const Vector4& rhs)
	{
		return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z && lhs.w == rhs.w;
	}

	friend bool operator!=(const Vector4& lhs, const Vector4& rhs)
	{
		return lhs.x != rhs.x || lhs.y != rhs.y || lhs.z != rhs.z || lhs.w != rhs.w;
	}

	static float Dot(const Vector4& a, const Vector4& b)
	{
		return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
	}

	Vector4 Negated()
	{
		return Vector4(-x, -y, -z, -w);
	}
};
