using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

#if _DEBUG
[assembly: InternalsVisibleTo("CoreTests")]
#endif

namespace Staple
{
    public class Transform : IComponent, IEnumerable<Transform>
    {
        private List<Transform> children = new List<Transform>();
        private Matrix4x4 matrix = Matrix4x4.Identity;
        private Quaternion rotation = Quaternion.Identity;
        private Vector3 position;
        private Vector3 scale = Vector3.One;

        public Transform parent { get; private set; }

        public Entity entity { get; internal set; }

        internal Matrix4x4 Matrix
        {
            get
            {
                if(Changed)
                {
                    Changed = false;

                    matrix = Matrix4x4.CreateScale(scale.X, scale.Y, scale.Z) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position);
                }

                if(parent != null)
                {
                    return matrix * parent.Matrix;
                }

                return matrix;
            }
        }

        public Vector3 Position
        {
            get
            {
                if(parent != null)
                {
                    return Vector3.Transform(position, parent.Matrix);
                }

                return position;
            }

            set
            {
                var parentPosition = parent?.Position ?? Vector3.Zero;

                position = value - parentPosition;

                Changed = true;
            }
        }

        public Vector3 LocalPosition
        {
            get => position;

            set
            {
                position = value;

                Changed = true;
            }
        }

        public Vector3 Scale
        {
            get
            {
                if (parent != null)
                {
                    return parent.Scale * scale;
                }

                return scale;
            }

            set
            {
                var parentScale = parent?.Scale ?? Vector3.One;

                scale = value / parentScale;

                Changed = true;
            }
        }

        public Vector3 LocalScale
        {
            get => scale;

            set
            {
                scale = value;

                Changed = true;
            }
        }

        public Quaternion Rotation
        {
            get
            {
                if(parent != null)
                {
                    return parent.Rotation + rotation;
                }

                return rotation;
            }
        }

        public Quaternion LocalRotation
        {
            get
            {
                return rotation;
            }

            set
            {
                rotation = value;

                Changed = true;
            }
        }

        public Vector3 Forward
        {
            get
            {
                return Vector3.Transform(new Vector3(0, 0, 1), Matrix4x4.CreateFromQuaternion(Rotation));
            }
        }

        public Vector3 Back
        {
            get
            {
                return Vector3.Transform(new Vector3(0, 0, -1), Matrix4x4.CreateFromQuaternion(Rotation));
            }
        }

        public Vector3 Up
        {
            get
            {
                return Vector3.Transform(new Vector3(0, 1, 0), Matrix4x4.CreateFromQuaternion(Rotation));
            }
        }

        public Vector3 Down
        {
            get
            {
                return Vector3.Transform(new Vector3(0, -1, 0), Matrix4x4.CreateFromQuaternion(Rotation));
            }
        }

        public Vector3 Left
        {
            get
            {
                return Vector3.Transform(new Vector3(-1, 0, 0), Matrix4x4.CreateFromQuaternion(Rotation));
            }
        }

        public Vector3 Right
        {
            get
            {
                return Vector3.Transform(new Vector3(1, 0, 0), Matrix4x4.CreateFromQuaternion(Rotation));
            }
        }

        public bool Changed { get; private set; } = false;

        public Transform Root
        {
            get
            {
                return parent != null ? parent.Root : this;
            }
        }

        public int ChildCount => children.Count;

        public int SiblingIndex => parent != null ? parent.ChildIndex(this) : 0;

        public bool SetSiblingIndex(int index) => parent?.MoveChild(this, index) ?? false;

        public Transform GetChild(int index) => index >= 0 && index < children.Count ? children[index] : null;

        public void SetParent(Transform parent)
        {
            if(this.parent != null)
            {
                this.parent.DetachChild(this);
            }

            this.parent = parent;

            if(parent != null)
            {
                parent.AttachChild(this);
            }
        }

        public IEnumerator<Transform> GetEnumerator()
        {
            return ((IEnumerable<Transform>)children).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)children).GetEnumerator();
        }

        private void DetachChild(Transform child)
        {
            if(children.Contains(child))
            {
                children.Remove(child);
            }
        }

        private void AttachChild(Transform child)
        {
            if(!children.Contains(child))
            {
                children.Add(child);
            }
        }

        private int ChildIndex(Transform child)
        {
            var index = children.IndexOf(child);

            if(index >= 0)
            {
                return index;
            }

            return 0;
        }

        private bool MoveChild(Transform child, int index)
        {
            if(children.Contains(child) && index < children.Count)
            {
                children.Remove(child);
                children.Insert(index, child);

                return true;
            }

            return false;
        }
    }
}
