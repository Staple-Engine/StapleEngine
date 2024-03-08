using System.Collections.Generic;
using System.Numerics;

namespace Staple.Utilities;

/// <summary>
/// Helper class for building cubic meshes.
/// You should always call the related functions together rather than mix them up.
/// For example, calling QuadVertices should be followed by CubeFaces (since they're compatible),
/// CubeVertices and CubeFaces, CrossVertices and CrossFaces, but not CubeVertices and CrossFaces.
/// </summary>
public class CubicMeshBuilder
{
    public enum Direction
    {
        Forward,
        Backward,
        Left,
        Right,
        Up,
        Down,
    }

    public List<Vector3> vertices = new();
    public List<Vector2> uvs = new();
    public List<int> indices = new();

    /// <summary>
    /// Adds quad vertices
    /// </summary>
    /// <param name="position">The position of the quad</param>
    /// <param name="size">The size of the quad</param>
    public void QuadVertices(Vector3 position, float size)
    {
        QuadVertices(position, Vector2.One * size);
    }

    /// <summary>
    /// Adds quad vertices
    /// </summary>
    /// <param name="position">The position of the quad</param>
    /// <param name="size">The size of the quad</param>
    public void QuadVertices(Vector3 position, Vector2 size)
    {
        var s = 0.5f * size;

        var newPositions = new Vector3[]
        {
            new Vector3(position.X - s.X, position.Y - s.Y, position.Z),
            new Vector3(position.X - s.X, position.Y + s.Y, position.Z),
            new Vector3(position.X + s.X, position.Y + s.Y, position.Z),
            new Vector3(position.X + s.X, position.Y - s.Y, position.Z),
        };

        vertices.AddRange(newPositions);
    }

    /// <summary>
    /// Adds cross vertices
    /// </summary>
    /// <param name="position">The position of the cross</param>
    /// <param name="size">The size of the cross</param>
    public void CrossVertices(Vector3 position, float size)
    {
        CrossVertices(position, Vector3.One * size);
    }

    /// <summary>
    /// Adds cross vertices
    /// </summary>
    /// <param name="position">The position of the cross</param>
    /// <param name="size">The size of the cross</param>
    public void CrossVertices(Vector3 position, Vector3 size)
    {
        var s = 0.5f * size;

        vertices.Add(new Vector3(position.X + s.X, position.Y - s.Y, position.Z + s.Z));
        vertices.Add(new Vector3(position.X + s.X, position.Y + s.Y, position.Z + s.Z));
        vertices.Add(new Vector3(position.X - s.X, position.Y + s.Y, position.Z - s.Z));
        vertices.Add(new Vector3(position.X - s.X, position.Y - s.Y, position.Z - s.Z));

        vertices.Add(new Vector3(position.X + s.X, position.Y - s.Y, position.Z - s.Z));
        vertices.Add(new Vector3(position.X + s.X, position.Y + s.Y, position.Z - s.Z));
        vertices.Add(new Vector3(position.X - s.X, position.Y + s.Y, position.Z + s.Z));
        vertices.Add(new Vector3(position.X - s.X, position.Y - s.Y, position.Z + s.Z));
    }

    /// <summary>
    /// Adds cube vertices
    /// </summary>
    /// <param name="position">The position of the cube</param>
    /// <param name="size">The size of the cube</param>
    /// <param name="direction">The direction to add the face to</param>
    public void CubeVertices(Vector3 position, float size, Direction direction)
    {
        CubeVertices(position, Vector3.One * size, direction);
    }

    /// <summary>
    /// Adds cube vertices
    /// </summary>
    /// <param name="position">The position of the cube</param>
    /// <param name="size">The size of the cube</param>
    /// <param name="direction">The direction to add the face to</param>
    public void CubeVertices(Vector3 position, Vector3 size, Direction direction)
    {
        var s = 0.5f * size;

        Vector3[] newPositions = null;

        switch (direction)
        {
            case Direction.Forward:

                newPositions = [
                    new Vector3(position.X + s.X, position.Y - s.Y, position.Z + s.Z),
                    new Vector3(position.X + s.X, position.Y + s.Y, position.Z + s.Z),
                    new Vector3(position.X - s.X, position.Y + s.Y, position.Z + s.Z),
                    new Vector3(position.X - s.X, position.Y - s.Y, position.Z + s.Z)
                ];

                break;

            case Direction.Backward:

                newPositions = [
                    new Vector3(position.X - s.X, position.Y - s.Y, position.Z - s.Z),
                    new Vector3(position.X - s.X, position.Y + s.Y, position.Z - s.Z),
                    new Vector3(position.X + s.X, position.Y + s.Y, position.Z - s.Z),
                    new Vector3(position.X + s.X, position.Y - s.Y, position.Z - s.Z),
                ];

                break;

            case Direction.Up:

                newPositions = [
                    new Vector3(position.X - s.X, position.Y + s.Y, position.Z + s.Z),
                    new Vector3(position.X + s.X, position.Y + s.Y, position.Z + s.Z),
                    new Vector3(position.X + s.X, position.Y + s.Y, position.Z - s.Z),
                    new Vector3(position.X - s.X, position.Y + s.Y, position.Z - s.Z),
                ];

                break;

            case Direction.Down:

                newPositions = [
                    new Vector3(position.X - s.X, position.Y - s.Y, position.Z - s.Z),
                    new Vector3(position.X + s.X, position.Y - s.Y, position.Z - s.Z),
                    new Vector3(position.X + s.X, position.Y - s.Y, position.Z + s.Z),
                    new Vector3(position.X - s.X, position.Y - s.Y, position.Z + s.Z),
                ];

                break;

            case Direction.Left:

                newPositions = [
                    new Vector3(position.X - s.X, position.Y - s.Y, position.Z + s.Z),
                    new Vector3(position.X - s.X, position.Y + s.Y, position.Z + s.Z),
                    new Vector3(position.X - s.X, position.Y + s.Y, position.Z - s.Z),
                    new Vector3(position.X - s.X, position.Y - s.Y, position.Z - s.Z),
                ];

                break;

            case Direction.Right:

                newPositions = [
                    new Vector3(position.X + s.X, position.Y - s.Y, position.Z - s.Z),
                    new Vector3(position.X + s.X, position.Y + s.Y, position.Z - s.Z),
                    new Vector3(position.X + s.X, position.Y + s.Y, position.Z + s.Z),
                    new Vector3(position.X + s.X, position.Y - s.Y, position.Z + s.Z),
                ];

                break;
        }

        if (newPositions == null)
        {
            return;
        }

        vertices.AddRange(newPositions);
    }

    /// <summary>
    /// Adds cube face indices
    /// </summary>
    /// <remarks>Works for quad as well</remarks>
    public void CubeFaces()
    {
        indices.Add(vertices.Count - 4);
        indices.Add(vertices.Count - 3);
        indices.Add(vertices.Count - 2);

        indices.Add(vertices.Count - 4);
        indices.Add(vertices.Count - 2);
        indices.Add(vertices.Count - 1);
    }

    /// <summary>
    /// Adds cross face indices
    /// </summary>
    public void CrossFaces()
    {
        indices.Add(vertices.Count - 8);
        indices.Add(vertices.Count - 7);
        indices.Add(vertices.Count - 6);

        indices.Add(vertices.Count - 8);
        indices.Add(vertices.Count - 6);
        indices.Add(vertices.Count - 5);

        indices.Add(vertices.Count - 4);
        indices.Add(vertices.Count - 3);
        indices.Add(vertices.Count - 2);

        indices.Add(vertices.Count - 4);
        indices.Add(vertices.Count - 2);
        indices.Add(vertices.Count - 1);
    }

    /// <summary>
    /// Adds a set of cross UVs
    /// </summary>
    /// <param name="texture">The coordinates of the texture to use</param>
    public void CrossTexture(RectFloat texture)
    {
        uvs.Add(new Vector2(texture.left, texture.top));
        uvs.Add(new Vector2(texture.left, texture.bottom));
        uvs.Add(new Vector2(texture.right, texture.bottom));
        uvs.Add(new Vector2(texture.right, texture.top));

        uvs.Add(new Vector2(texture.left, texture.top));
        uvs.Add(new Vector2(texture.left, texture.bottom));
        uvs.Add(new Vector2(texture.right, texture.bottom));
        uvs.Add(new Vector2(texture.right, texture.top));
    }

    /// <summary>
    /// Adds a set of cube UVs
    /// </summary>
    /// <param name="texture">The coordinates of the texture to use</param>
    /// <param name="rotation">The rotation to apply to the face</param>
    public void CubeTexture(RectFloat texture, int rotation = 0)
    {
        switch (rotation)
        {
            case 0:

                uvs.Add(new Vector2(texture.left, texture.top));
                uvs.Add(new Vector2(texture.left, texture.bottom));
                uvs.Add(new Vector2(texture.right, texture.bottom));
                uvs.Add(new Vector2(texture.right, texture.top));

                break;

            case 1:

                uvs.Add(new Vector2(texture.left, texture.bottom));
                uvs.Add(new Vector2(texture.right, texture.bottom));
                uvs.Add(new Vector2(texture.right, texture.top));
                uvs.Add(new Vector2(texture.left, texture.top));

                break;

            case 2:

                uvs.Add(new Vector2(texture.right, texture.bottom));
                uvs.Add(new Vector2(texture.right, texture.top));
                uvs.Add(new Vector2(texture.left, texture.top));
                uvs.Add(new Vector2(texture.left, texture.bottom));

                break;

            case 3:

                uvs.Add(new Vector2(texture.right, texture.top));
                uvs.Add(new Vector2(texture.left, texture.top));
                uvs.Add(new Vector2(texture.left, texture.bottom));
                uvs.Add(new Vector2(texture.right, texture.bottom));

                break;
        }
    }

    /// <summary>
    /// Creates a mesh from the data in this builder
    /// </summary>
    /// <param name="addUVs">Whether to add UV coordinates to the mesh</param>
    /// <returns>The mesh</returns>
    public Mesh BuildMesh(bool addUVs)
    {
        var mesh = new Mesh(true, true)
        {
            indexFormat = MeshIndexFormat.UInt32,
            vertices = vertices.ToArray(),
            indices = indices.ToArray(),
            meshTopology = MeshTopology.Triangles,
            changed = true,
        };

        if (addUVs)
        {
            mesh.uv = uvs.ToArray();
        }

        return mesh;
    }
}
