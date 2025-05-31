#pragma once

#include "ufbx.h"

class Matrix4x4
{
public:
	float m00, m01, m02, m03;
	float m10, m11, m12, m13;
	float m20, m21, m22, m23;
	float m30, m31, m32, m33;

	Matrix4x4() : m00(0), m01(0), m02(0), m03(0),
		m10(0), m11(0), m12(0), m13(0),
		m20(0), m21(0), m22(0), m23(0),
		m30(0), m31(0), m32(0), m33(0) {}

	Matrix4x4(const ufbx_matrix &m) : m00(m.m00), m01(m.m01), m02(m.m02), m03(m.m03),
		m10(m.m10), m11(m.m11), m12(m.m12), m13(m.m13),
		m20(m.m20), m21(m.m21), m22(m.m22), m23(m.m23),
		m30(0), m31(0), m32(0), m33(1) {}
};
