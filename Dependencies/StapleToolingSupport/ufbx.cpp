#include <vector>
#include <cstdlib>
#include "common.h"
#include "ufbx.h"
#include "Math/Math.hpp"

#define DELETE(array)\
	if(array != nullptr)\
	{\
		delete [] array;\
		array = nullptr;\
	}

#define COPY(to, from, elementType, count)\
	if(from != nullptr)\
	{\
		to = new elementType[count];\
		memcpy(to, from, sizeof(elementType) * count);\
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

	if (ptr == nullptr)
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

	if (ptr == nullptr)
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

class Vertex
{
public:
	Vector3 position;
	Vector3 normal;
	Vector3 tangent;
	Vector3 bitangent;
	Vector2 uv0;
	Vector2 uv1;
	Vector2 uv2;
	Vector2 uv3;
	Vector2 uv4;
	Vector2 uv5;
	Vector2 uv6;
	Vector2 uv7;
	Vector4 color0;
	Vector4 color1;
	Vector4 color2;
	Vector4 color3;
	Vector4 boneIndices;
	Vector4 boneWeights;
};

class MeshBone
{
public:
	int32_t nodeIndex;
	Matrix4x4 offsetMatrix;

	MeshBone() : nodeIndex(-1)
	{
	}
};

class Mesh
{
public:
	Vector3* vertices;
	Vector3* normals;
	Vector3* tangents;
	Vector3* bitangents;
	Vector2* uv0;
	Vector2* uv1;
	Vector2* uv2;
	Vector2* uv3;
	Vector2* uv4;
	Vector2* uv5;
	Vector2* uv6;
	Vector2* uv7;
	Vector4* color0;
	Vector4* color1;
	Vector4* color2;
	Vector4* color3;
	Vector4* boneIndices;
	Vector4* boneWeights;

	int32_t vertexCount;

	uint32_t* indices;

	int32_t indexCount;

	int32_t materialIndex;

	bool isSkinned;

	MeshBone* bones;

	int32_t boneCount;

	Mesh() : vertices(nullptr), normals(nullptr), tangents(nullptr), bitangents(nullptr),
		uv0(nullptr), uv1(nullptr), uv2(nullptr), uv3(nullptr),
		uv4(nullptr), uv5(nullptr), uv6(nullptr), uv7(nullptr),
		color0(nullptr), color1(nullptr), color2(nullptr), color3(nullptr),
		boneIndices(nullptr), boneWeights(nullptr), vertexCount(0),
		indices(nullptr), indexCount(0), materialIndex(-1), isSkinned(false),
		bones(nullptr), boneCount(0) {
	}

	Mesh(const Mesh& o) : vertices(nullptr), normals(nullptr), tangents(nullptr), bitangents(nullptr),
		uv0(nullptr), uv1(nullptr), uv2(nullptr), uv3(nullptr),
		uv4(nullptr), uv5(nullptr), uv6(nullptr), uv7(nullptr),
		color0(nullptr), color1(nullptr), color2(nullptr), color3(nullptr),
		boneIndices(nullptr), boneWeights(nullptr), vertexCount(o.vertexCount),
		indices(nullptr), indexCount(o.indexCount), materialIndex(o.materialIndex), isSkinned(o.isSkinned),
		bones(nullptr), boneCount(o.boneCount)
	{
		COPY(vertices, o.vertices, Vector3, vertexCount);
		COPY(normals, o.normals, Vector3, vertexCount);
		COPY(tangents, o.tangents, Vector3, vertexCount);
		COPY(bitangents, o.bitangents, Vector3, vertexCount);
		COPY(uv0, o.uv0, Vector2, vertexCount);
		COPY(uv1, o.uv1, Vector2, vertexCount);
		COPY(uv2, o.uv2, Vector2, vertexCount);
		COPY(uv3, o.uv3, Vector2, vertexCount);
		COPY(uv4, o.uv4, Vector2, vertexCount);
		COPY(uv5, o.uv5, Vector2, vertexCount);
		COPY(uv6, o.uv6, Vector2, vertexCount);
		COPY(uv7, o.uv7, Vector2, vertexCount);
		COPY(color0, o.color0, Vector4, vertexCount);
		COPY(color1, o.color1, Vector4, vertexCount);
		COPY(color2, o.color2, Vector4, vertexCount);
		COPY(color3, o.color3, Vector4, vertexCount);
		COPY(boneIndices, o.boneIndices, Vector4, vertexCount);
		COPY(boneWeights, o.boneWeights, Vector4, vertexCount);
		COPY(bones, o.bones, MeshBone, boneCount);
		COPY(indices, o.indices, uint32_t, indexCount);
	}

	~Mesh()
	{
		DELETE(vertices);
		DELETE(normals);
		DELETE(tangents);
		DELETE(bitangents);
		DELETE(uv0);
		DELETE(uv1);
		DELETE(uv2);
		DELETE(uv3);
		DELETE(uv4);
		DELETE(uv5);
		DELETE(uv6);
		DELETE(uv7);
		DELETE(color0);
		DELETE(color1);
		DELETE(color2);
		DELETE(color3);
		DELETE(indices);
		DELETE(bones);
	}

	Mesh& operator=(const Mesh& o)
	{
		DELETE(vertices);
		DELETE(normals);
		DELETE(tangents);
		DELETE(bitangents);
		DELETE(uv0);
		DELETE(uv1);
		DELETE(uv2);
		DELETE(uv3);
		DELETE(uv4);
		DELETE(uv5);
		DELETE(uv6);
		DELETE(uv7);
		DELETE(color0);
		DELETE(color1);
		DELETE(color2);
		DELETE(color3);
		DELETE(indices);
		DELETE(bones);

		vertexCount = o.vertexCount;
		indexCount = o.indexCount;
		boneCount = o.boneCount;
		materialIndex = o.materialIndex;
		isSkinned = o.isSkinned;

		COPY(vertices, o.vertices, Vector3, vertexCount);
		COPY(normals, o.normals, Vector3, vertexCount);
		COPY(tangents, o.tangents, Vector3, vertexCount);
		COPY(bitangents, o.bitangents, Vector3, vertexCount);
		COPY(uv0, o.uv0, Vector2, vertexCount);
		COPY(uv1, o.uv1, Vector2, vertexCount);
		COPY(uv2, o.uv2, Vector2, vertexCount);
		COPY(uv3, o.uv3, Vector2, vertexCount);
		COPY(uv4, o.uv4, Vector2, vertexCount);
		COPY(uv5, o.uv5, Vector2, vertexCount);
		COPY(uv6, o.uv6, Vector2, vertexCount);
		COPY(uv7, o.uv7, Vector2, vertexCount);
		COPY(color0, o.color0, Vector4, vertexCount);
		COPY(color1, o.color1, Vector4, vertexCount);
		COPY(color2, o.color2, Vector4, vertexCount);
		COPY(color3, o.color3, Vector4, vertexCount);
		COPY(boneIndices, o.boneIndices, Vector4, vertexCount);
		COPY(boneWeights, o.boneWeights, Vector4, vertexCount);
		COPY(bones, o.bones, MeshBone, boneCount);
		COPY(indices, o.indices, uint32_t, indexCount);

		return *this;
	}
};

class String
{
public:
	char data[10240];
	int32_t length;

	String() : length(0)
	{
		memset(data, 0, sizeof(data));
	}

	String(const char* ptr)
	{
		this->length = (int32_t)strlen(ptr);

		memset(data, 0, sizeof(data));
		memcpy(data, ptr, this->length);
	}

	String(const char* ptr, size_t length)
	{
		this->length = length > 10240 ? 1024 : (int32_t)length;

		memset(data, 0, sizeof(data));
		memcpy(data, ptr, this->length);
	}

	String(const String& o) : length(o.length)
	{
		memcpy(data, o.data, sizeof(data));
	}

	String& operator=(const String& o)
	{
		length = o.length;

		memcpy(data, o.data, sizeof(data));

		return *this;
	}
};

class Node
{
public:

	int32_t parentIndex;

	String name;

	int32_t* meshIndices;

	int32_t meshCount;

	Transform localTransform;

	Matrix4x4 geometryToNode;
	Matrix4x4 nodeToParent;
	Matrix4x4 nodeToWorld;
	Matrix4x4 geometryToWorld;
	Matrix4x4 normalToWorld;

	Node() : parentIndex(-1), meshIndices(nullptr), meshCount(0)
	{
	}

	void Read(ufbx_scene *scene, ufbx_node* node, std::vector<Mesh> &meshCache)
	{
		if (node->name.length == 0)
		{
			if (node->is_root)
			{
				name = String("internal_root");
			}
			else
			{
				name = String("internal_group");
			}
		}
		else
		{
			name = String(node->name.data, node->name.length);
		}

		parentIndex = node->parent ? node->parent->typed_id : -1;

		localTransform.position = Vector3(node->local_transform.translation);
		localTransform.rotation = Vector4(node->local_transform.rotation);
		localTransform.scale = Vector3(node->local_transform.scale);

		nodeToParent = Matrix4x4(node->node_to_parent);
		nodeToWorld = Matrix4x4(node->node_to_world);
		geometryToNode = Matrix4x4(node->geometry_to_node);
		geometryToWorld = Matrix4x4(node->geometry_to_world);
		normalToWorld = Matrix4x4(ufbx_matrix_for_normals(&node->geometry_to_world));

		if (node->mesh != nullptr)
		{
			ufbx_mesh* mesh = node->mesh;

			std::vector<Mesh> meshes;

			std::vector<int32_t> meshIndices;

			for (size_t j = 0; j < mesh->material_parts.count; j++)
			{
				const ufbx_mesh_part& part = mesh->material_parts[j];

				if (part.num_triangles == 0)
				{
					continue;
				}

				Mesh ownMesh;

				ownMesh.isSkinned = mesh->skin_deformers.count > 0;

				ufbx_material *material = node->materials[j];

				ufbx_skin_deformer* skin = mesh->skin_deformers.count > 0 ? mesh->skin_deformers[0] : nullptr;

				std::vector<Vertex> vertices;

				bool hasTangents = false;
				bool hasBitangents = false;
				bool hasColors[4] = { 0 };
				bool hasUVs[8] = { 0 };

				std::vector<MeshBone> bones;

				for (size_t faceIndex = 0; faceIndex < part.num_faces; faceIndex++)
				{
					const ufbx_face& face = mesh->faces[part.face_indices.data[faceIndex]];

					size_t triangleIndexCount = mesh->max_face_triangles * 3;

					std::vector<uint32_t> indices(triangleIndexCount);

					size_t triangleCount = ufbx_triangulate_face(indices.data(), triangleIndexCount, mesh, face);

					size_t vertexCount = triangleCount * 3;

					for(size_t k = 0; k < vertexCount; k++)
					{
						uint32_t index = indices[k];

						Vertex v;

						uint32_t vertexIndex = mesh->vertex_indices[index];

						v.position = Vector3(mesh->vertices[vertexIndex]);

						{
							uint32_t normalIndex = mesh->vertex_normal.indices[index];

							if (normalIndex < mesh->vertex_normal.values.count)
							{
								v.normal = Vector3(mesh->vertex_normal.values[normalIndex]);
							}
						}

						if (mesh->vertex_tangent.exists)
						{
							hasTangents = true;

							uint32_t tangentIndex = mesh->vertex_tangent.indices[index];

							if (tangentIndex < mesh->vertex_tangent.values.count)
							{
								v.tangent = Vector3(mesh->vertex_tangent.values[tangentIndex]);
							}
						}

						if (mesh->vertex_bitangent.exists)
						{
							hasBitangents = true;

							uint32_t bitangentIndex = mesh->vertex_bitangent.indices[index];

							if (bitangentIndex < mesh->vertex_bitangent.values.count)
							{
								v.tangent = Vector3(mesh->vertex_bitangent.values[bitangentIndex]);
							}
						}

						uint32_t uvCount = mesh->uv_sets.count < 8 ? (uint32_t)mesh->uv_sets.count : 8;

						Vector2* uvs[8] =
						{
							&v.uv0,
							&v.uv1,
							&v.uv2,
							&v.uv3,
							&v.uv4,
							&v.uv5,
							&v.uv6,
							&v.uv7,
						};

						hasUVs[0] = uvCount >= 1;
						hasUVs[1] = uvCount >= 2;
						hasUVs[2] = uvCount >= 3;
						hasUVs[3] = uvCount >= 4;
						hasUVs[4] = uvCount >= 5;
						hasUVs[5] = uvCount >= 6;
						hasUVs[6] = uvCount >= 7;
						hasUVs[7] = uvCount >= 8;

						for (uint32_t l = 0; l < uvCount; l++)
						{
							const ufbx_uv_set& UVSet = mesh->uv_sets[l];

							uint32_t UVIndex = UVSet.vertex_uv.indices[index];

							if (UVIndex < UVSet.vertex_uv.values.count)
							{
								*uvs[l] = Vector2(UVSet.vertex_uv.values[UVIndex]);
							}
						}

						Vector4* colors[4]
						{
							&v.color0,
							&v.color1,
							&v.color2,
							&v.color3,
						};

						uint32_t colorCount = mesh->color_sets.count < 4 ? (uint32_t)mesh->color_sets.count : 4;

						hasColors[0] = colorCount >= 1;
						hasColors[1] = colorCount >= 2;
						hasColors[2] = colorCount >= 3;
						hasColors[3] = colorCount >= 4;

						for (uint32_t l = 0; l < colorCount; l++)
						{
							const ufbx_color_set& colorSet = mesh->color_sets[l];

							uint32_t colorIndex = colorSet.vertex_color.indices[index];

							if (colorIndex < colorSet.vertex_color.values.count)
							{
								*colors[l] = colorSet.vertex_color.values[colorIndex];
							}
						}

						if (skin != nullptr)
						{
							const ufbx_skin_vertex& skinVertex = skin->vertices[vertexIndex];

							uint32_t weightCount = skinVertex.num_weights <= 4 ? skinVertex.num_weights : 4;

							float* boneIndices[4] =
							{
								&v.boneIndices.x,
								&v.boneIndices.y,
								&v.boneIndices.z,
								&v.boneIndices.w,
							};

							float* boneWeights[4] =
							{
								&v.boneWeights.x,
								&v.boneWeights.y,
								&v.boneWeights.z,
								&v.boneWeights.w,
							};

							float weightSum = 0.0f;

							for (uint32_t l = 0; l < weightCount; l++)
							{
								const ufbx_skin_weight &weight = skin->weights[skinVertex.weight_begin + l];

								uint32_t clusterIndex = weight.cluster_index;

								ufbx_skin_cluster *cluster = skin->clusters[clusterIndex];

								uint32_t nodeIndex = cluster->bone_node->typed_id;

								bool found = false;

								for (const MeshBone &bone : bones)
								{
									if (bone.nodeIndex == nodeIndex)
									{
										found = true;

										break;
									}
								}

								if (found == false)
								{
									MeshBone bone;

									bone.nodeIndex = nodeIndex;
									bone.offsetMatrix = cluster->geometry_to_bone;

									bones.push_back(bone);
								}

								float jointIndex = (float)clusterIndex;
								float w = weight.weight;

								weightSum += w;

								*boneIndices[l] = jointIndex;
								*boneWeights[l] = w;
							}

							if (weightSum > 0)
							{
								v.boneWeights.x /= weightSum;
								v.boneWeights.y /= weightSum;
								v.boneWeights.z /= weightSum;
								v.boneWeights.w /= weightSum;
							}
						}

						vertices.push_back(v);
					}
				}

				ufbx_vertex_stream vertexStream = { 0 };

				vertexStream.data = vertices.data();
				vertexStream.vertex_count = vertices.size();
				vertexStream.vertex_size = sizeof(Vertex);

				std::vector<uint32_t> indices(vertices.size());

				ufbx_error indexError;

				uint32_t vertexCount = (uint32_t)ufbx_generate_indices(&vertexStream, 1, indices.data(), vertices.size(), nullptr, &indexError);

				if (indexError.type != UFBX_ERROR_NONE)
				{
					PrintError(&indexError, "Mesh index generation failed");
				}
				else
				{
					ownMesh.vertices = new Vector3[vertexCount];
					ownMesh.normals = new Vector3[vertexCount];
					ownMesh.tangents = hasTangents ? new Vector3[vertexCount] : nullptr;
					ownMesh.bitangents = hasBitangents ? new Vector3[vertexCount] : nullptr;
					ownMesh.color0 = hasColors[0] ? new Vector4[vertexCount] : nullptr;
					ownMesh.color1 = hasColors[1] ? new Vector4[vertexCount] : nullptr;
					ownMesh.color2 = hasColors[2] ? new Vector4[vertexCount] : nullptr;
					ownMesh.color3 = hasColors[3] ? new Vector4[vertexCount] : nullptr;
					ownMesh.uv0 = hasUVs[0] ? new Vector2[vertexCount] : nullptr;
					ownMesh.uv1 = hasUVs[1] ? new Vector2[vertexCount] : nullptr;
					ownMesh.uv2 = hasUVs[2] ? new Vector2[vertexCount] : nullptr;
					ownMesh.uv3 = hasUVs[3] ? new Vector2[vertexCount] : nullptr;
					ownMesh.uv4 = hasUVs[4] ? new Vector2[vertexCount] : nullptr;
					ownMesh.uv5 = hasUVs[5] ? new Vector2[vertexCount] : nullptr;
					ownMesh.uv6 = hasUVs[6] ? new Vector2[vertexCount] : nullptr;
					ownMesh.uv7 = hasUVs[7] ? new Vector2[vertexCount] : nullptr;
					ownMesh.boneIndices = skin != nullptr ? new Vector4[vertexCount] : nullptr;
					ownMesh.boneWeights = skin != nullptr ? new Vector4[vertexCount] : nullptr;

					ownMesh.vertexCount = vertexCount;
					ownMesh.indexCount = (int32_t)indices.size();
					ownMesh.materialIndex = material->typed_id;

					ownMesh.indices = new uint32_t[indices.size()];

					memcpy(ownMesh.indices, indices.data(), indices.size() * sizeof(uint32_t));

					if (bones.size() > 0)
					{
						ownMesh.boneCount = (int32_t)bones.size();

						ownMesh.bones = new MeshBone[ownMesh.boneCount];

						for (size_t k = 0; k < ownMesh.boneCount; k++)
						{
							ownMesh.bones[k] = bones[k];
						}
					}

					for (size_t k = 0; k < vertexCount; k++)
					{
						const Vertex& v = vertices[k];

						ownMesh.vertices[k] = v.position;
						ownMesh.normals[k] = v.normal;

#define COPYIF(condition, to, from)\
	if(condition)\
	{\
		to[k] = from;\
	}

						COPYIF(hasTangents, ownMesh.tangents, v.tangent);
						COPYIF(hasBitangents, ownMesh.bitangents, v.bitangent);
						COPYIF(hasColors[0], ownMesh.color0, v.color0);
						COPYIF(hasColors[1], ownMesh.color1, v.color1);
						COPYIF(hasColors[2], ownMesh.color2, v.color2);
						COPYIF(hasColors[3], ownMesh.color3, v.color3);
						COPYIF(hasUVs[0], ownMesh.uv0, v.uv0);
						COPYIF(hasUVs[1], ownMesh.uv1, v.uv1);
						COPYIF(hasUVs[2], ownMesh.uv2, v.uv2);
						COPYIF(hasUVs[3], ownMesh.uv3, v.uv3);
						COPYIF(hasUVs[4], ownMesh.uv4, v.uv4);
						COPYIF(hasUVs[5], ownMesh.uv5, v.uv5);
						COPYIF(hasUVs[6], ownMesh.uv6, v.uv6);
						COPYIF(hasUVs[7], ownMesh.uv7, v.uv7);
						COPYIF(skin != nullptr, ownMesh.boneIndices, v.boneIndices);
						COPYIF(skin != nullptr, ownMesh.boneWeights, v.boneWeights);
#undef COPYIF
					}

					meshIndices.push_back((int32_t)meshCache.size());

					meshCache.push_back(ownMesh);
				}
			}

			if (meshIndices.size() > 0)
			{
				this->meshCount = (int32_t)meshIndices.size();
				this->meshIndices = new int32_t[meshIndices.size()];

				memcpy(this->meshIndices, meshIndices.data(), meshIndices.size() * sizeof(int32_t));
			}
		}
	}
};

enum TextureWrap : int
{
	STAPLE_TEXTURE_WRAP_REPEAT,
	STAPLE_TEXTURE_WRAP_CLAMP,
	STAPLE_TEXTURE_WRAP_MIRROR,
};

static TextureWrap UFBXWrapToStapleWrap(ufbx_wrap_mode mode)
{
	switch (mode)
	{
	case UFBX_WRAP_CLAMP:

		return STAPLE_TEXTURE_WRAP_CLAMP;

	case UFBX_WRAP_REPEAT:

		return STAPLE_TEXTURE_WRAP_REPEAT;

	default:

		return STAPLE_TEXTURE_WRAP_CLAMP;
	}
}

class Material
{
public:

	String name;

#define MATERIALPROP(name) \
	Vector4 name ## Color;\
	String name ## Texture;\
	TextureWrap name ## WrapU; \
	TextureWrap name ## WrapV;

	MATERIALPROP(diffuse);
	MATERIALPROP(specular);
	MATERIALPROP(reflection);
	MATERIALPROP(transparency);
	MATERIALPROP(emission);
	MATERIALPROP(ambient);
	MATERIALPROP(normalMap);
	MATERIALPROP(bump);
	MATERIALPROP(displacement);
	MATERIALPROP(vectorDisplacement);

#undef MATERIALPROP

	Material() : diffuseWrapU(STAPLE_TEXTURE_WRAP_CLAMP), diffuseWrapV(STAPLE_TEXTURE_WRAP_CLAMP),
		specularWrapU(STAPLE_TEXTURE_WRAP_CLAMP), specularWrapV(STAPLE_TEXTURE_WRAP_CLAMP),
		reflectionWrapU(STAPLE_TEXTURE_WRAP_CLAMP), reflectionWrapV(STAPLE_TEXTURE_WRAP_CLAMP),
		transparencyWrapU(STAPLE_TEXTURE_WRAP_CLAMP), transparencyWrapV(STAPLE_TEXTURE_WRAP_CLAMP),
		emissionWrapU(STAPLE_TEXTURE_WRAP_CLAMP), emissionWrapV(STAPLE_TEXTURE_WRAP_CLAMP),
		ambientWrapU(STAPLE_TEXTURE_WRAP_CLAMP), ambientWrapV(STAPLE_TEXTURE_WRAP_CLAMP),
		normalMapWrapU(STAPLE_TEXTURE_WRAP_CLAMP), normalMapWrapV(STAPLE_TEXTURE_WRAP_CLAMP),
		bumpWrapU(STAPLE_TEXTURE_WRAP_CLAMP), bumpWrapV(STAPLE_TEXTURE_WRAP_CLAMP),
		displacementWrapU(STAPLE_TEXTURE_WRAP_CLAMP), displacementWrapV(STAPLE_TEXTURE_WRAP_CLAMP),
		vectorDisplacementWrapU(STAPLE_TEXTURE_WRAP_CLAMP), vectorDisplacementWrapV(STAPLE_TEXTURE_WRAP_CLAMP)
	{
	}

	Material(const Material& o) : name(o.name),
		diffuseTexture(o.diffuseTexture), diffuseWrapU(STAPLE_TEXTURE_WRAP_CLAMP), diffuseWrapV(STAPLE_TEXTURE_WRAP_CLAMP),
		specularTexture(o.specularTexture), specularWrapU(STAPLE_TEXTURE_WRAP_CLAMP), specularWrapV(STAPLE_TEXTURE_WRAP_CLAMP),
		reflectionTexture(o.reflectionTexture), reflectionWrapU(STAPLE_TEXTURE_WRAP_CLAMP), reflectionWrapV(STAPLE_TEXTURE_WRAP_CLAMP),
		transparencyTexture(o.transparencyTexture), transparencyWrapU(STAPLE_TEXTURE_WRAP_CLAMP), transparencyWrapV(STAPLE_TEXTURE_WRAP_CLAMP),
		emissionTexture(o.emissionTexture), emissionWrapU(STAPLE_TEXTURE_WRAP_CLAMP), emissionWrapV(STAPLE_TEXTURE_WRAP_CLAMP),
		ambientTexture(o.ambientTexture), ambientWrapU(STAPLE_TEXTURE_WRAP_CLAMP), ambientWrapV(STAPLE_TEXTURE_WRAP_CLAMP),
		normalMapTexture(o.normalMapTexture), normalMapWrapU(STAPLE_TEXTURE_WRAP_CLAMP), normalMapWrapV(STAPLE_TEXTURE_WRAP_CLAMP),
		bumpTexture(o.bumpTexture), bumpWrapU(STAPLE_TEXTURE_WRAP_CLAMP), bumpWrapV(STAPLE_TEXTURE_WRAP_CLAMP),
		displacementTexture(o.displacementTexture), displacementWrapU(STAPLE_TEXTURE_WRAP_CLAMP), displacementWrapV(STAPLE_TEXTURE_WRAP_CLAMP),
		vectorDisplacementTexture(o.vectorDisplacementTexture), vectorDisplacementWrapU(STAPLE_TEXTURE_WRAP_CLAMP),
		vectorDisplacementWrapV(STAPLE_TEXTURE_WRAP_CLAMP)
	{
	}

	void Read(ufbx_material* material)
	{
		if (material->name.length > 0)
		{
			name = String(material->name.data, material->name.length);
		}

#define MATERIALCOLOR(to, map)\
		if (material->fbx.maps[map].has_value)\
		{\
			to ## Color = material->fbx.maps[map].value_vec4;\
		}\

#define MATERIALTEXTURE(to, map)\
		if (material->fbx.maps[map].has_value && material->fbx.maps[map].texture != nullptr)\
		{\
			ufbx_texture *texture = material->fbx.maps[map].texture; \
			const ufbx_string &fileName = texture->filename; \
			\
			if(fileName.length > 0)\
			{\
				to ## Texture = String(fileName.data, fileName.length); \
			}\
			\
			to ## WrapU = UFBXWrapToStapleWrap(texture->wrap_u); \
			to ## WrapV = UFBXWrapToStapleWrap(texture->wrap_v); \
		}

#define DOMATERIAL(to, map)\
		MATERIALCOLOR(to, map);\
		\
		MATERIALTEXTURE(to, map);

		DOMATERIAL(diffuse, UFBX_MATERIAL_FBX_DIFFUSE_COLOR);
		DOMATERIAL(specular, UFBX_MATERIAL_FBX_SPECULAR_COLOR);
		DOMATERIAL(reflection, UFBX_MATERIAL_FBX_REFLECTION_COLOR);
		DOMATERIAL(transparency, UFBX_MATERIAL_FBX_TRANSPARENCY_COLOR);
		DOMATERIAL(emission, UFBX_MATERIAL_FBX_EMISSION_COLOR);
		DOMATERIAL(ambient, UFBX_MATERIAL_FBX_AMBIENT_COLOR);
		DOMATERIAL(normalMap, UFBX_MATERIAL_FBX_NORMAL_MAP);
		DOMATERIAL(bump, UFBX_MATERIAL_FBX_BUMP);
		DOMATERIAL(displacement, UFBX_MATERIAL_FBX_DISPLACEMENT);
		DOMATERIAL(vectorDisplacement, UFBX_MATERIAL_FBX_VECTOR_DISPLACEMENT);

#undef DOMATERIAL
#undef MATERIALTEXTURE
#undef MATERIALCOLOR
	}
};

class Scene
{
public:
	Node* nodes;

	int32_t nodeCount;

	Mesh* meshes;

	int32_t meshCount;

	Material* materials;

	int32_t materialCount;

	Scene() : nodes(nullptr), nodeCount(0), meshes(nullptr), meshCount(0), materials(nullptr), materialCount(0) {}

	~Scene()
	{
		DELETE(nodes);
		DELETE(meshes);
		DELETE(materials);
	}

	void Read(ufbx_scene* scene)
	{
		nodeCount = (int32_t)scene->nodes.count;

		nodes = new Node[nodeCount];

		std::vector<Mesh> meshCache;

		for (int32_t i = 0; i < nodeCount; i++)
		{
			nodes[i].Read(scene, scene->nodes.data[i], meshCache);
		}

		meshCount = (int32_t)meshCache.size();

		if (meshCount > 0)
		{
			meshes = new Mesh[meshCount];

			for (int32_t i = 0; i < meshCount; i++)
			{
				meshes[i] = meshCache[i];
			}
		}

		materialCount = (int32_t)scene->materials.count;

		if (materialCount > 0)
		{
			materials = new Material[materialCount];

			for (int32_t i = 0; i < materialCount; i++)
			{
				materials[i].Read(scene->materials[i]);
			}
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

	if (scene == nullptr)
	{
		PrintError(&error, "Failed to load scene");

		return nullptr;
	}

	Scene* ownScene = new Scene();

	ownScene->Read(scene);

	ufbx_free_scene(scene);

	return ownScene;
}

CEXPORT void UFBXFreeScene(Scene* ptr)
{
	if (ptr == nullptr)
	{
		return;
	}

	delete ptr;
}
