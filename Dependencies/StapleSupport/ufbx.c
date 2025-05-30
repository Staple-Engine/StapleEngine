#include <stdlib.h>
#include "common.h"
#include "ufbx.h"

typedef struct
{
	float x, y;
} Vector2;

typedef struct
{
	float x, y, z;
} Vector3;

typedef struct
{
	float x, y, z, w;
} Vector4;

typedef struct
{
	float m00, m01, m02, m03;
	float m10, m11, m12, m13;
	float m20, m21, m22, m23;
	float m30, m31, m32, m33;
} Matrix4x4;

typedef struct
{
	Vector3 position;
	Vector3 normal;
	Vector2 uv;
} MeshVertex;

typedef struct
{
	MeshVertex vertex;
	Vector4 boneIndex;
	Vector4 boneWeight;
} MeshSkinVertex;

static Vector2 UFBXVec2ToVector2(ufbx_vec2 v)
{
	Vector2 out = { 0 };

	out.x = v.x;
	out.y = v.y;

	return out;
}

static Vector3 UFBXVec3ToVector3(ufbx_vec3 v)
{
	Vector3 out = { 0 };

	out.x = v.x;
	out.y = v.y;
	out.z = v.z;

	return out;
}

static Vector4 UFBXVec4ToVector4(ufbx_vec4 v)
{
	Vector4 out = { 0 };

	out.x = v.x;
	out.y = v.y;
	out.z = v.z;
	out.w = v.w;

	return out;
}

static Vector4 UFBXQuatToVector4(ufbx_quat v)
{
	Vector4 out = { 0 };

	out.x = v.x;
	out.y = v.y;
	out.z = v.z;
	out.w = v.w;

	return out;
}

static Matrix4x4 UFBXMatToMatrix4x4(ufbx_matrix matrix)
{
	Matrix4x4 out = { 0 };

	out.m00 = matrix.m00;
	out.m01 = matrix.m01;
	out.m02 = matrix.m02;
	out.m03 = matrix.m03;

	out.m10 = matrix.m10;
	out.m11 = matrix.m11;
	out.m12 = matrix.m12;
	out.m13 = matrix.m13;

	out.m20 = matrix.m20;
	out.m21 = matrix.m21;
	out.m22 = matrix.m22;
	out.m23 = matrix.m23;

	out.m30 = 0;
	out.m31 = 0;
	out.m32 = 0;
	out.m33 = 1;

	return out;
}

static void PrintError(const ufbx_error* error, const char* description)
{
	char buffer[1024];

	ufbx_format_error(buffer, sizeof(buffer), error);

	printf("ERROR: %s\n%s\n", description, buffer);
}

static void* AllocImpl(size_t typeSize, size_t count)
{
	void* ptr = malloc(typeSize * count);

	if (ptr == NULL)
	{
		printf("ERROR: Out of Memory!\n");

		exit(1);
	}

	memset(ptr, 0, typeSize * count);

	return ptr;
}

static void* AllocDupImpl(size_t typeSize, size_t count, const void* data)
{
	void* ptr = malloc(typeSize * count);

	if (ptr == NULL)
	{
		printf("ERROR: Out of Memory!\n");

		exit(1);
	}

	memcpy(ptr, data, typeSize * count);

	return ptr;
}

#define Alloc(m_type, m_count) (m_type *)AllocImpl(sizeof(m_type), (m_count))
#define AllocDup(m_type, m_count, m_data) (m_type *)AllocDupImpl(sizeof(m_type), (m_count), (m_data))

static size_t MinSize(size_t a, size_t b)
{
	return a < b ? a : b;
}

static size_t MaxSize(size_t a, size_t b)
{
	return b > a ? b : a;
}

static size_t ClampSize(size_t a, size_t min, size_t max)
{
	return MinSize(MaxSize(a, min), max);
}

typedef struct
{
	int32_t parentIndex;

	Matrix4x4 geometryToNode;
	Matrix4x4 nodeToParent;
	Matrix4x4 nodeToWorld;
	Matrix4x4 geometryToWorld;
	Matrix4x4 normalToWorld;
} Node;

typedef struct
{
	Node* nodes;

	size_t nodeCount;
} Scene;

static void ReadNode(Node* ownNode, ufbx_node* node)
{
	ownNode->parentIndex = node->parent ? node->parent->typed_id : -1;
	ownNode->nodeToParent = UFBXMatToMatrix4x4(node->node_to_parent);
	ownNode->nodeToWorld = UFBXMatToMatrix4x4(node->node_to_world);
	ownNode->geometryToNode = UFBXMatToMatrix4x4(node->geometry_to_node);
	ownNode->geometryToWorld = UFBXMatToMatrix4x4(node->geometry_to_world);
	ownNode->normalToWorld = UFBXMatToMatrix4x4(ufbx_matrix_for_normals(&node->geometry_to_world));
}

static void ReadScene(Scene* ownScene, ufbx_scene* scene)
{
	ownScene->nodeCount = scene->nodes.count;

	ownScene->nodes = Alloc(Node, ownScene->nodeCount);

	for (size_t i = 0; i < ownScene->nodeCount; i++)
	{
		ReadNode(&ownScene->nodes[i], scene->nodes.data[i]);
	}
}

EXPORT Scene* UFBXLoadScene(const char* fileName)
{
	ufbx_load_opts opts = {

		.load_external_files = true,
		.ignore_missing_external_files = true,
		.generate_missing_normals = true,

		.evaluate_skinning = true,

		.target_axes = {

			.right = UFBX_COORDINATE_AXIS_POSITIVE_X,
			.up = UFBX_COORDINATE_AXIS_POSITIVE_Y,
			.front = UFBX_COORDINATE_AXIS_POSITIVE_Z,
		},

		.target_unit_meters = 1.0f,
	};

	ufbx_error error;

	ufbx_scene* scene = ufbx_load_file(fileName, &opts, &error);

	if (scene == NULL)
	{
		PrintError(&error, "Failed to load scene");

		return NULL;
	}

	Scene* ownScene = Alloc(Scene, 1);

	ReadScene(ownScene, scene);

	ufbx_free_scene(scene);

	return ownScene;
}

EXPORT void UFBXFreeScene(Scene* ptr)
{
	if (ptr == NULL)
	{
		return;
	}

	free(ptr);
}
