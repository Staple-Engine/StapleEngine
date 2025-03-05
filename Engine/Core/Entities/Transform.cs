using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Staple;

/// <summary>
/// Transform component.
/// Contains rotation, position, scale, and parent entity.
/// </summary>
[AutoAssignEntity]
public class Transform : IComponent
{
    internal bool changed = false;

    /// <summary>
    /// Whether the transform has changed.
    /// This is used to force children to refresh themselves when the parent is modified.
    /// </summary>
    internal bool Changed
    {
        get => changed;

        set
        {
            var wasChanged = changed;

            changed = value;

            if(changed && World.Current != null)
            {
                World.Current.Changed = true;
            }

            if(wasChanged == false && changed)
            {
                for(var i = 0; i < Children.Length; i++)
                {
                    Children[i].Changed = true;
                }
            }
        }
    }

    /// <summary>
    /// Child transforms
    /// </summary>
    public Transform[] Children { get; private set; } = [];

    /// <summary>
    /// Our transform matrix
    /// </summary>
    private Matrix4x4 matrix = Matrix4x4.Identity;

    /// <summary>
    /// Local rotation
    /// </summary>
    private Quaternion rotation = Quaternion.Identity;

    /// <summary>
    /// Local position
    /// </summary>
    private Vector3 position;

    /// <summary>
    /// Local scale
    /// </summary>
    private Vector3 scale = Vector3.One;

    /// <summary>
    /// Global transform matrix
    /// </summary>
    private Matrix4x4 finalMatrix = Matrix4x4.Identity;

    /// <summary>
    /// Global position
    /// </summary>
    private Vector3 finalPosition;

    /// <summary>
    /// Global scale
    /// </summary>
    private Vector3 finalScale;

    /// <summary>
    /// Global rotation
    /// </summary>
    private Quaternion finalRotation;

    /// <summary>
    /// The parent of this transform, if any.
    /// </summary>
    public Transform parent { get; private set; }

    /// <summary>
    /// The entity this transform belongs to
    /// </summary>
    public Entity entity { get; internal set; }

    /// <summary>
    /// Gets the transform's Global Transformation Matrix
    /// </summary>
    public Matrix4x4 Matrix
    {
        get
        {
            UpdateState();

            return finalMatrix;
        }
    }

    /// <summary>
    /// The world-space position
    /// </summary>
    public Vector3 Position
    {
        get
        {
            UpdateState();

            return finalPosition;
        }

        set
        {
            Changed = true;

            var parentPosition = parent?.Position ?? Vector3.Zero;

            position = value - parentPosition;
        }
    }

    /// <summary>
    /// The local-space position
    /// </summary>
    public Vector3 LocalPosition
    {
        get => position;

        set
        {
            Changed = true;

            position = value;
        }
    }

    /// <summary>
    /// The world-space scale
    /// </summary>
    public Vector3 Scale
    {
        get
        {
            UpdateState();

            return finalScale;
        }

        set
        {
            Changed = true;

            var parentScale = parent?.Scale ?? Vector3.One;

            scale = value / parentScale;
        }
    }

    /// <summary>
    /// The local-space scale
    /// </summary>
    public Vector3 LocalScale
    {
        get => scale;

        set
        {
            Changed = true;

            scale = value;
        }
    }

    /// <summary>
    /// The world-space rotation
    /// </summary>
    public Quaternion Rotation
    {
        get
        {
            UpdateState();

            return finalRotation;
        }

        set
        {
            Changed = true;

            var parentRotation = parent?.Rotation ?? Quaternion.Identity;

            rotation = Quaternion.Inverse(parentRotation) * value;
        }
    }

    /// <summary>
    /// The local-space rotation
    /// </summary>
    public Quaternion LocalRotation
    {
        get => rotation;

        set
        {
            Changed = true;

            rotation = value;
        }
    }

    /// <summary>
    /// The forward direction
    /// </summary>
    public Vector3 Forward => Vector3.Normalize(Vector3.Transform(new Vector3(0, 0, -1), Rotation));

    /// <summary>
    /// The backwards direction
    /// </summary>
    public Vector3 Back => Vector3.Normalize(Vector3.Transform(new Vector3(0, 0, 1), Rotation));

    /// <summary>
    /// The up direction
    /// </summary>
    public Vector3 Up => Vector3.Normalize(Vector3.Transform(new Vector3(0, 1, 0), Rotation));

    /// <summary>
    /// The down direction
    /// </summary>
    public Vector3 Down => Vector3.Normalize(Vector3.Transform(new Vector3(0, -1, 0), Rotation));

    /// <summary>
    /// The left direction
    /// </summary>
    public Vector3 Left => Vector3.Normalize(Vector3.Transform(new Vector3(-1, 0, 0), Rotation));

    /// <summary>
    /// The right direction
    /// </summary>
    public Vector3 Right => Vector3.Normalize(Vector3.Transform(new Vector3(1, 0, 0), Rotation));

    /// <summary>
    /// The root transform of this transform
    /// </summary>
    public Transform Root => parent != null ? parent.Root : this;

    /// <summary>
    /// The total children in this transform
    /// </summary>
    public int ChildCount => Children.Length;

    /// <summary>
    /// The index of this transform in its parent
    /// </summary>
    public int SiblingIndex => parent != null ? parent.ChildIndex(this) : 0;

    /// <summary>
    /// Sets this transform's index in its parent
    /// </summary>
    /// <param name="index">The new index</param>
    /// <returns>Whether this was moved</returns>
    public bool SetSiblingIndex(int index) => parent?.MoveChild(this, index) ?? false;

    /// <summary>
    /// Gets a child at a specific index
    /// </summary>
    /// <param name="index">The index of the child</param>
    /// <returns>The child, or null</returns>
    public Transform GetChild(int index) => index >= 0 && index < Children.Length ? Children[index] : null;

    /// <summary>
    /// Searches for a child transform with a specific name and optional partial search
    /// </summary>
    /// <param name="name">The name of the child</param>
    /// <param name="partial">Whether the search should be partial. If so, it will search as a prefix</param>
    /// <returns>The child transform, or null</returns>
    public Transform SearchChild(string name, bool partial = false)
    {
        if(ChildCount == 0)
        {
            return null;
        }

        foreach(var child in Children)
        {
            if(partial && child.entity.Name.StartsWith(name, System.StringComparison.Ordinal))
            {
                return child;
            }

            if(child.entity.Name == name)
            {
                return child;
            }

            var result = child.SearchChild(name, partial);

            if(result != null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Sets this transform's parent
    /// </summary>
    /// <param name="parent">The new parent (can be null to remove)</param>
    public void SetParent(Transform parent)
    {
        if (parent == this)
        {
            return;
        }

        this.parent?.DetachChild(this);

        this.parent = parent;

        parent?.AttachChild(this);

        Changed = true;

        Scene.RequestWorldUpdate();
    }

    /// <summary>
    /// Ensures we have the latest state for this transform
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateState()
    {
        if (changed)
        {
            changed = false;

            matrix = Math.TransformationMatrix(position, scale, rotation);

            finalMatrix = parent != null ? matrix * parent.Matrix : matrix;

            finalPosition = parent != null ? Vector3.Transform(position, parent.Matrix) : position;
            finalRotation = parent != null ? parent.Rotation * rotation : rotation;
            finalScale = parent != null ? parent.Scale * scale : scale;
        }
    }

    /// <summary>
    /// Detaches a child from our children list
    /// </summary>
    /// <param name="child">The child to detach</param>
    private void DetachChild(Transform child)
    {
        var newChildren = new Transform[Children.Length - 1];

        for(int i = 0, current = 0; i < Children.Length; i++)
        {
            if (Children[i] == child)
            {
                continue;
            }

            newChildren[current++] = Children[i];
        }

        Children = newChildren;
    }

    /// <summary>
    /// Attaches a child to this transform
    /// </summary>
    /// <param name="child">The new child</param>
    private void AttachChild(Transform child)
    {
        var newChildren = new Transform[Children.Length + 1];

        Array.Copy(Children, newChildren, Children.Length);

        newChildren[Children.Length] = child;

        Children = newChildren;
    }

    /// <summary>
    /// Gets the index of a child (used exclusively in the sibling index property)
    /// </summary>
    /// <param name="child">The child</param>
    /// <returns>The index, or 0</returns>
    private int ChildIndex(Transform child)
    {
        for(var i = 0; i < Children.Length; i++)
        {
            if(Children[i] == child)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Moves a child in our children list
    /// </summary>
    /// <param name="child">The child</param>
    /// <param name="index">The new index</param>
    /// <returns>Whether it was successfully moved</returns>
    private bool MoveChild(Transform child, int index)
    {
        var childIndex = ChildIndex(child);

        if(childIndex < 0 || childIndex >= Children.Length)
        {
            return false;
        }

        (Children[childIndex], Children[index]) = (Children[index], Children[childIndex]);

        Scene.RequestWorldUpdate();

        return true;
    }
}
