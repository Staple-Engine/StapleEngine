#include <stdlib.h>
#include "common.h"
#include "ufbx.h"
#include "Math/Math.hpp"

class MeshVertex
{
public:
	Vector3 position;
	Vector3 normal;
	Vector2 uv;
};

class MeshSkinVertex
{
public:
	MeshVertex vertex;
	Vector4 boneIndex;
	Vector4 boneWeight;
};

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

class Node
{
public:

	int32_t parentIndex;

	char name[10240];

	int32_t nameLength;

	Transform localTransform;

	Matrix4x4 geometryToNode;
	Matrix4x4 nodeToParent;
	Matrix4x4 nodeToWorld;
	Matrix4x4 geometryToWorld;
	Matrix4x4 normalToWorld;

	Node() : parentIndex(-1), nameLength(0)
	{
		memset(name, 0, sizeof(name));
	}

	void Read(ufbx_node* node)
	{
		if (node->name.length == 0)
		{
			if (node->is_root)
			{
				strncpy_s(name, "StapleRoot", strlen("StapleRoot"));
			}
		}
		else
		{
			strncpy_s(name, node->name.data, node->name.length > sizeof(name) ? sizeof(name) : node->name.length);
		}

		nameLength = strnlen(name, sizeof(name));

		parentIndex = node->parent ? node->parent->typed_id : -1;

		localTransform.position = Vector3(node->local_transform.translation);
		localTransform.rotation = Vector4(node->local_transform.rotation);
		localTransform.scale = Vector3(node->local_transform.scale);

		nodeToParent = Matrix4x4(node->node_to_parent);
		nodeToWorld = Matrix4x4(node->node_to_world);
		geometryToNode = Matrix4x4(node->geometry_to_node);
		geometryToWorld = Matrix4x4(node->geometry_to_world);
		normalToWorld = Matrix4x4(ufbx_matrix_for_normals(&node->geometry_to_world));
	}
};

class Scene
{
public:
	Node* nodes;

	size_t nodeCount;

	Scene() : nodes(NULL), nodeCount(0) {}

	~Scene()
	{
		if (nodes != NULL)
		{
			delete[] nodes;

			nodes = NULL;
		}
	}

	void Read(ufbx_scene* scene)
	{
		nodeCount = scene->nodes.count;

		nodes = new Node[nodeCount];

		for (size_t i = 0; i < nodeCount; i++)
		{
			nodes[i].Read(scene->nodes.data[i]);
		}
	}
};

CEXPORT Scene* UFBXLoadScene(const char* fileName)
{
	ufbx_load_opts opts;

	memset(&opts, 0, sizeof(opts));

	opts.load_external_files = true;
	opts.ignore_missing_external_files = true;
	opts.generate_missing_normals = true;

	opts.evaluate_skinning = true;

	opts.target_axes.right = UFBX_COORDINATE_AXIS_POSITIVE_X;
	opts.target_axes.up = UFBX_COORDINATE_AXIS_POSITIVE_Y;
	opts.target_axes.front = UFBX_COORDINATE_AXIS_POSITIVE_Z;

	opts.target_unit_meters = 1.0f;

	ufbx_error error;

	ufbx_scene* scene = ufbx_load_file(fileName, &opts, &error);

	if (scene == NULL)
	{
		PrintError(&error, "Failed to load scene");

		return NULL;
	}

	Scene* ownScene = new Scene();

	ownScene->Read(scene);

	ufbx_free_scene(scene);

	return ownScene;
}

CEXPORT void UFBXFreeScene(Scene* ptr)
{
	if (ptr == NULL)
	{
		return;
	}

	delete ptr;
}
