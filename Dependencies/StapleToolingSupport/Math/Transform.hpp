#pragma once

#include "Vector3.hpp"
#include "Vector4.hpp"

class Transform
{
public:
	Vector3 position;
	Vector4 rotation;
	Vector3 scale;
};
