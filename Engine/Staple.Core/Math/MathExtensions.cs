using System.Numerics;

namespace Staple;

public static class FloatExtensions
{
    extension(float)
    {
        /// <summary>
        /// Moves a float towards a specific value over time. This method will never overshoot the target.
        /// </summary>
        /// <param name="current">The current value</param>
        /// <param name="target">The value we want to reach</param>
        /// <param name="currentVelocity">The current velocity. This is always changed every time you call this function</param>
        /// <param name="smoothTime">The time you expect to reach the target</param>
        /// <param name="maxSpeed">The maximum speed to reach the target</param>
        /// <param name="deltaTime">The time since the last call</param>
        /// <returns>The value</returns>
        public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime,
            float maxSpeed, float deltaTime)
        {
            smoothTime = Math.Max(0.0001f, smoothTime);

            var omega = 2.0f / smoothTime;
            var x = omega * deltaTime;
            var exponential = 1.0f / (1.0f + x + 0.48f * x * x + 0.235f * x * x * x);

            var change = current - target;
            var original = target;

            var max = maxSpeed * smoothTime;
            change = Math.Clamp(change, -max, max);
            target = current - change;

            var temp = (currentVelocity + omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exponential;

            var output = target + (change + temp) * exponential;

            if((original - current > 0) == (output > original))
            {
                output = original;
                currentVelocity = (output - original) / deltaTime;
            }

            return output;
        }

        /// <summary>
        /// Moves a float towards a specific value over time. This method will never overshoot the target.
        /// </summary>
        /// <param name="current">The current value</param>
        /// <param name="target">The value we want to reach</param>
        /// <param name="currentVelocity">The current velocity. This is always changed every time you call this function</param>
        /// <param name="smoothTime">The time you expect to reach the target</param>
        /// <param name="maxSpeed">The maximum speed to reach the target</param>
        /// <returns>The value</returns>
        public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime,
            float maxSpeed)
        {
            return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, Time.deltaTime);
        }

        /// <summary>
        /// Moves a float towards a specific value over time. This method will never overshoot the target.
        /// </summary>
        /// <param name="current">The current value</param>
        /// <param name="target">The value we want to reach</param>
        /// <param name="currentVelocity">The current velocity. This is always changed every time you call this function</param>
        /// <param name="smoothTime">The time you expect to reach the target</param>
        /// <returns>The value</returns>
        public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime)
        {
            return SmoothDamp(current, target, ref currentVelocity, smoothTime, Math.Infinity, Time.deltaTime);
        }
    }
}

public static class Vector2Extensions
{
    extension(Vector2 v)
    {
        public Vector2 Normalized => Vector2.Normalize(v);

        public float Dot(Vector2 other) => Vector2.Dot(v, other);

        public Vector2 Transformed(Matrix4x4 matrix) => Vector2.Transform(v, matrix);

        public Vector2 Transformed(Quaternion quaternion) => Vector2.Transform(v, quaternion);

        /// <summary>
        /// Returns a copy of this as a Vector3
        /// </summary>
        /// <returns>The Vector3</returns>
        public Vector3 ToVector3() => new(v.X, v.Y, 0);

        /// <summary>
        /// Returns a copy of this as a Vector4
        /// </summary>
        /// <param name="transform">Whether to keep the transform portion of the vector</param>
        /// <returns>The Vector4</returns>
        public Vector4 ToVector4(bool transform = false)
        {
            return new Vector4(v.X, v.Y, 0, transform ? 1 : 0);
        }

        /// <summary>
        /// Calculates the angle between two vectors
        /// </summary>
        /// <param name="from">The vector we want to check the angle from</param>
        /// <param name="to">The vector we want to check the angle to</param>
        /// <returns>The angle, or 0 if too small</returns>
        public static float Angle(Vector2 from, Vector2 to)
        {
            var dot = Vector2.Dot(from, to);
            var magnitude = from.Length() * to.Length();

            if (magnitude < Math.ZeroTolerance)
            {
                return 0;
            }

            return Math.Acos(dot / magnitude) * Math.Rad2Deg;
        }

        /// <summary>
        /// Checks if this is close to equal to another Vector2
        /// </summary>
        /// <param name="b">The other</param>
        /// <param name="delta">The distance we want to check</param>
        /// <returns>Whether it's close to equal</returns>
        public bool IsCloseToEqual(Vector2 b, float delta = Math.ZeroTolerance)
        {
            return Math.Abs(v.X - b.X) < delta &&
                Math.Abs(v.Y - b.Y) < delta;
        }

        /// <summary>
        /// Moves a Vector2 towards a specific value over time. This method will never overshoot the target.
        /// </summary>
        /// <param name="current">The current value</param>
        /// <param name="target">The value we want to reach</param>
        /// <param name="currentVelocity">The current velocity. This is always changed every time you call this function</param>
        /// <param name="smoothTime">The time you expect to reach the target</param>
        /// <param name="maxSpeed">The maximum speed to reach the target</param>
        /// <param name="deltaTime">The time since the last call</param>
        /// <returns>The value</returns>
        public static Vector2 SmoothDamp(Vector2 current, Vector2 target, ref Vector2 currentVelocity, float smoothTime,
            float maxSpeed, float deltaTime)
        {
            return new(float.SmoothDamp(current.X, target.X, ref currentVelocity.X, smoothTime, maxSpeed, deltaTime),
                float.SmoothDamp(current.Y, target.Y, ref currentVelocity.Y, smoothTime, maxSpeed, deltaTime));
        }

        /// <summary>
        /// Moves a Vector2 towards a specific value over time. This method will never overshoot the target.
        /// </summary>
        /// <param name="current">The current value</param>
        /// <param name="target">The value we want to reach</param>
        /// <param name="currentVelocity">The current velocity. This is always changed every time you call this function</param>
        /// <param name="smoothTime">The time you expect to reach the target</param>
        /// <param name="maxSpeed">The maximum speed to reach the target</param>
        /// <returns>The value</returns>
        public static Vector2 SmoothDamp(Vector2 current, Vector2 target, ref Vector2 currentVelocity, float smoothTime,
            float maxSpeed)
        {
            return new(float.SmoothDamp(current.X, target.X, ref currentVelocity.X, smoothTime, maxSpeed),
                float.SmoothDamp(current.Y, target.Y, ref currentVelocity.Y, smoothTime, maxSpeed));
        }

        /// <summary>
        /// Moves a Vector2 towards a specific value over time. This method will never overshoot the target.
        /// </summary>
        /// <param name="current">The current value</param>
        /// <param name="target">The value we want to reach</param>
        /// <param name="currentVelocity">The current velocity. This is always changed every time you call this function</param>
        /// <param name="smoothTime">The time you expect to reach the target</param>
        /// <returns>The value</returns>
        public static Vector2 SmoothDamp(Vector2 current, Vector2 target, ref Vector2 currentVelocity, float smoothTime)
        {
            return new(float.SmoothDamp(current.X, target.X, ref currentVelocity.X, smoothTime),
                float.SmoothDamp(current.Y, target.Y, ref currentVelocity.Y, smoothTime));
        }
    }
}

public static class Vector3Extensions
{
    extension(Vector3 v)
    {
        public static Vector3 Right => Vector3.UnitX;

        public static Vector3 Up => Vector3.UnitY;

        public static Vector3 Forward => Vector3.UnitZ;

        public static Vector3 Left => -Vector3.UnitX;

        public static Vector3 Down => -Vector3.UnitY;

        public static Vector3 Back => -Vector3.UnitZ;

        public Vector3 Normalized => Vector3.Normalize(v);

        public float Dot(Vector3 other) => Vector3.Dot(v, other);

        public Vector3 Transformed(Matrix4x4 matrix) => Vector3.Transform(v, matrix);

        public Vector3 Transformed(Quaternion quaternion) => Vector3.Transform(v, quaternion);

        public Vector3 Cross(Vector3 other) => Vector3.Cross(v, other);

        /// <summary>
        /// Returns a copy of this as a Vector2
        /// </summary>
        /// <returns>The Vector2</returns>
        public Vector2 ToVector2() => new(v.X, v.Y);

        /// <summary>
        /// Returns a copy of this as a Vector4
        /// </summary>
        /// <param name="transform">Whether to keep the transform portion of the vector</param>
        /// <returns>The Vector4</returns>
        public Vector4 ToVector4(bool transform = false)
        {
            return new Vector4(v.X, v.Y, v.Z, transform ? 1 : 0);
        }

        /// <summary>
        /// Calculates the angle between two vectors
        /// </summary>
        /// <param name="from">The vector we want to check the angle from</param>
        /// <param name="to">The vector we want to check the angle to</param>
        /// <returns>The angle, or 0 if too small</returns>
        public static float Angle(Vector3 from, Vector3 to)
        {
            var dot = from.Dot(to);
            var magnitude = from.Length() * to.Length();

            if (magnitude < Math.ZeroTolerance)
            {
                return 0;
            }

            return Math.Acos(dot / magnitude) * Math.Rad2Deg;
        }

        /// <summary>
        /// Checks if this is close to equal to another Vector3
        /// </summary>
        /// <param name="b">The other</param>
        /// <param name="delta">The distance we want to check</param>
        /// <returns>Whether it's close to equal</returns>
        public bool IsCloseToEqual(Vector3 b, float delta = Math.ZeroTolerance)
        {
            return Math.Abs(v.X - b.X) < delta &&
                Math.Abs(v.Y - b.Y) < delta &&
                Math.Abs(v.Z - b.Z) < delta;
        }

        /// <summary>
        /// Moves a Vector3 towards a specific value over time. This method will never overshoot the target.
        /// </summary>
        /// <param name="current">The current value</param>
        /// <param name="target">The value we want to reach</param>
        /// <param name="currentVelocity">The current velocity. This is always changed every time you call this function</param>
        /// <param name="smoothTime">The time you expect to reach the target</param>
        /// <param name="maxSpeed">The maximum speed to reach the target</param>
        /// <param name="deltaTime">The time since the last call</param>
        /// <returns>The value</returns>
        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime,
            float maxSpeed, float deltaTime)
        {
            return new(float.SmoothDamp(current.X, target.X, ref currentVelocity.X, smoothTime, maxSpeed, deltaTime),
                float.SmoothDamp(current.Y, target.Y, ref currentVelocity.Y, smoothTime, maxSpeed, deltaTime),
                float.SmoothDamp(current.Z, target.Z, ref currentVelocity.Z, smoothTime, maxSpeed, deltaTime));
        }

        /// <summary>
        /// Moves a Vector3 towards a specific value over time. This method will never overshoot the target.
        /// </summary>
        /// <param name="current">The current value</param>
        /// <param name="target">The value we want to reach</param>
        /// <param name="currentVelocity">The current velocity. This is always changed every time you call this function</param>
        /// <param name="smoothTime">The time you expect to reach the target</param>
        /// <param name="maxSpeed">The maximum speed to reach the target</param>
        /// <returns>The value</returns>
        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime,
            float maxSpeed)
        {
            return new(float.SmoothDamp(current.X, target.X, ref currentVelocity.X, smoothTime, maxSpeed),
                float.SmoothDamp(current.Y, target.Y, ref currentVelocity.Y, smoothTime, maxSpeed),
                float.SmoothDamp(current.Z, target.Z, ref currentVelocity.Z, smoothTime, maxSpeed));
        }

        /// <summary>
        /// Moves a Vector3 towards a specific value over time. This method will never overshoot the target.
        /// </summary>
        /// <param name="current">The current value</param>
        /// <param name="target">The value we want to reach</param>
        /// <param name="currentVelocity">The current velocity. This is always changed every time you call this function</param>
        /// <param name="smoothTime">The time you expect to reach the target</param>
        /// <returns>The value</returns>
        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime)
        {
            return new(float.SmoothDamp(current.X, target.X, ref currentVelocity.X, smoothTime),
                float.SmoothDamp(current.Y, target.Y, ref currentVelocity.Y, smoothTime),
                float.SmoothDamp(current.Z, target.Z, ref currentVelocity.Z, smoothTime));
        }
    }
}

public static class Vector4Extensions
{
    extension(Vector4 v)
    {
        public static Vector4 Right => Vector4.UnitX;

        public static Vector4 Up => Vector4.UnitY;

        public static Vector4 Forward => Vector4.UnitZ;

        public static Vector4 Left => -Vector4.UnitX;

        public static Vector4 Down => -Vector4.UnitY;

        public static Vector4 Back => -Vector4.UnitZ;

        public Vector4 Normalized => Vector4.Normalize(v);

        public float Dot(Vector4 other) => Vector4.Dot(v, other);

        public Vector4 Transformed(Matrix4x4 matrix) => Vector4.Transform(v, matrix);

        public Vector4 Transformed(Quaternion quaternion) => Vector4.Transform(v, quaternion);

        public Vector4 Cross(Vector4 other) => Vector4.Cross(v, other);

        /// <summary>
        /// Returns a copy of this as a Vector2
        /// </summary>
        /// <returns>The Vector2</returns>
        public Vector2 ToVector2() => new(v.X, v.Y);

        /// <summary>
        /// Returns a copy of this as a Vector3
        /// </summary>
        /// <returns>The Vector3</returns>
        public Vector3 ToVector3() => new(v.X, v.Y, v.Z);

        /// <summary>
        /// Calculates the angle between two vectors
        /// </summary>
        /// <param name="from">The vector we want to check the angle from</param>
        /// <param name="to">The vector we want to check the angle to</param>
        /// <returns>The angle, or 0 if too small</returns>
        public static float Angle(Vector4 from, Vector4 to)
        {
            var dot = from.Dot(to);
            var magnitude = from.Length() * to.Length();

            if (magnitude < Math.ZeroTolerance)
            {
                return 0;
            }

            return Math.Acos(dot / magnitude) * Math.Rad2Deg;
        }

        /// <summary>
        /// Checks if this is close to equal to another Vector4
        /// </summary>
        /// <param name="b">The other</param>
        /// <param name="delta">The distance we want to check</param>
        /// <returns>Whether it's close to equal</returns>
        public bool IsCloseToEqual(Vector4 b, float delta = Math.ZeroTolerance)
        {
            return Math.Abs(v.X - b.X) < delta &&
                Math.Abs(v.Y - b.Y) < delta &&
                Math.Abs(v.Z - b.Z) < delta &&
                Math.Abs(v.W - b.W) < delta;
        }

        /// <summary>
        /// Moves a Vector4 towards a specific value over time. This method will never overshoot the target.
        /// </summary>
        /// <param name="current">The current value</param>
        /// <param name="target">The value we want to reach</param>
        /// <param name="currentVelocity">The current velocity. This is always changed every time you call this function</param>
        /// <param name="smoothTime">The time you expect to reach the target</param>
        /// <param name="maxSpeed">The maximum speed to reach the target</param>
        /// <param name="deltaTime">The time since the last call</param>
        /// <returns>The value</returns>
        public static Vector4 SmoothDamp(Vector4 current, Vector4 target, ref Vector4 currentVelocity, float smoothTime,
            float maxSpeed, float deltaTime)
        {
            return new(float.SmoothDamp(current.X, target.X, ref currentVelocity.X, smoothTime, maxSpeed, deltaTime),
                float.SmoothDamp(current.Y, target.Y, ref currentVelocity.Y, smoothTime, maxSpeed, deltaTime),
                float.SmoothDamp(current.Z, target.Z, ref currentVelocity.Z, smoothTime, maxSpeed, deltaTime),
                float.SmoothDamp(current.W, target.W, ref currentVelocity.W, smoothTime, maxSpeed, deltaTime));
        }

        /// <summary>
        /// Moves a Vector4 towards a specific value over time. This method will never overshoot the target.
        /// </summary>
        /// <param name="current">The current value</param>
        /// <param name="target">The value we want to reach</param>
        /// <param name="currentVelocity">The current velocity. This is always changed every time you call this function</param>
        /// <param name="smoothTime">The time you expect to reach the target</param>
        /// <param name="maxSpeed">The maximum speed to reach the target</param>
        /// <returns>The value</returns>
        public static Vector4 SmoothDamp(Vector4 current, Vector4 target, ref Vector4 currentVelocity, float smoothTime,
            float maxSpeed)
        {
            return new(float.SmoothDamp(current.X, target.X, ref currentVelocity.X, smoothTime, maxSpeed),
                float.SmoothDamp(current.Y, target.Y, ref currentVelocity.Y, smoothTime, maxSpeed),
                float.SmoothDamp(current.Z, target.Z, ref currentVelocity.Z, smoothTime, maxSpeed),
                float.SmoothDamp(current.W, target.W, ref currentVelocity.W, smoothTime, maxSpeed));
        }

        /// <summary>
        /// Moves a Vector4 towards a specific value over time. This method will never overshoot the target.
        /// </summary>
        /// <param name="current">The current value</param>
        /// <param name="target">The value we want to reach</param>
        /// <param name="currentVelocity">The current velocity. This is always changed every time you call this function</param>
        /// <param name="smoothTime">The time you expect to reach the target</param>
        /// <returns>The value</returns>
        public static Vector4 SmoothDamp(Vector4 current, Vector4 target, ref Vector4 currentVelocity, float smoothTime)
        {
            return new(float.SmoothDamp(current.X, target.X, ref currentVelocity.X, smoothTime),
                float.SmoothDamp(current.Y, target.Y, ref currentVelocity.Y, smoothTime),
                float.SmoothDamp(current.Z, target.Z, ref currentVelocity.Z, smoothTime),
                float.SmoothDamp(current.W, target.W, ref currentVelocity.W, smoothTime));
        }
    }
}

public static class Matrix4x4Extensions
{
    extension(Matrix4x4 m)
    {
        /// <summary>
        /// Returns a copy of this as a Matrix3x3
        /// </summary>
        /// <returns>The 3x3 matrix</returns>
        public Matrix3x3 ToMatrix3x3()
        {
            return new()
            {
                M11 = m.M11,
                M12 = m.M12,
                M13 = m.M13,
                M21 = m.M21,
                M22 = m.M22,
                M23 = m.M23,
                M31 = m.M31,
                M32 = m.M32,
                M33 = m.M33,
            };
        }

        /// <summary>
        /// Creates a transformation matrix
        /// </summary>
        /// <param name="position">The position</param>
        /// <param name="scale">The scale</param>
        /// <param name="rotation">The rotation</param>
        /// <returns>The transformation matrix</returns>
        public static Matrix4x4 TRS(Vector3 position, Vector3 scale, Quaternion rotation)
        {
            return Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position);
        }

        /// <summary>
        /// Creates a left-handed orthographic projection matrix
        /// </summary>
        /// <param name="left">left coordinate</param>
        /// <param name="right">right coordinate</param>
        /// <param name="bottom">bottom coordinate</param>
        /// <param name="top">top coordinate</param>
        /// <param name="zNear">near plane</param>
        /// <param name="zFar">far plane</param>
        /// <returns>The matrix</returns>
        public static Matrix4x4 OrthoLeftHanded(float left, float right, float bottom, float top, float zNear, float zFar)
        {
            var outValue = Matrix4x4.Identity;

            outValue.M11 = 2.0f / (right - left);
            outValue.M22 = 2.0f / (bottom - top);
            outValue.M33 = 1.0f / (zFar - zNear);
            outValue.M41 = (left + right) / (left - right);
            outValue.M42 = (top + bottom) / (bottom - top);
            outValue.M43 = zNear / (zNear - zFar);

            return outValue;
        }

        /// <summary>
        /// Creates a right-handed orthographic projection matrix
        /// </summary>
        /// <param name="left">left coordinate</param>
        /// <param name="right">right coordinate</param>
        /// <param name="bottom">bottom coordinate</param>
        /// <param name="top">top coordinate</param>
        /// <param name="zNear">near plane</param>
        /// <param name="zFar">far plane</param>
        /// <returns>The matrix</returns>
        public static Matrix4x4 OrthoRightHanded(float left, float right, float bottom, float top, float zNear, float zFar)
        {
            var outValue = Matrix4x4.Identity;

            outValue.M11 = 2.0f / (right - left);
            outValue.M22 = 2.0f / (top - bottom);
            outValue.M33 = 1.0f / (zNear - zFar);
            outValue.M41 = (left + right) / (left - right);
            outValue.M42 = (top + bottom) / (bottom - top);
            outValue.M43 = zNear / (zNear - zFar);

            return outValue;
        }

        /// <summary>
        /// Gets a matrix4x4 containing the 3x3 portion of this matrix
        /// </summary>
        /// <returns>A matrix4x4 containing the 3x3 portion of this matrix</returns>
        public Matrix4x4 ContainingMatrix3x3()
        {
            var outValue = m;

            outValue.M14 = outValue.M24 = outValue.M34 = outValue.M41 = outValue.M42 = outValue.M43 = 0;
            outValue.M44 = 1;

            return outValue;
        }
    }
}

public static class QuaternionExtensions
{
    extension(Quaternion q)
    {
        public Quaternion Normalized => Quaternion.Normalize(q);

        public float Dot(Quaternion other) => Quaternion.Dot(q, other);

        /// <summary>
        /// Returns a copy of this quaternion as euler angles
        /// </summary>
        /// <returns>The angles as a vector3 of degrees</returns>
        public Vector3 ToEulerAngles()
        {
            var outValue = new Vector3();

            float squareX = q.X * q.X;
            float squareY = q.Y * q.Y;
            float squareZ = q.Z * q.Z;
            float squareW = q.W * q.W;
            float unit = squareX + squareY + squareZ + squareW;
            float test = q.X * q.W - q.Y * q.Z;

            static float MatchBounds(float x)
            {
                while (x < -360)
                {
                    x += 360;
                }

                while (x > 360)
                {
                    x -= 360;
                }

                return x;
            }

            Vector3 Normalize()
            {
                outValue.X = MatchBounds(Math.Rad2Deg * outValue.X);
                outValue.Y = MatchBounds(Math.Rad2Deg * outValue.Y);
                outValue.Z = MatchBounds(Math.Rad2Deg * outValue.Z);

                return outValue;
            }

            if (test > 0.4995f * unit)
            {
                outValue.Y = 2.0f * Math.Atan2(q.Y, q.X);
                outValue.X = Math.PI / 2;

                return Normalize();
            }
            else if (test < -0.4995f * unit)
            {
                outValue.Y = -2.0f * Math.Atan2(q.Y, q.X);
                outValue.X = -Math.PI / 2;

                return Normalize();
            }

            var q2 = new Quaternion(q.W, q.Z, q.X, q.Y);

            outValue.X = Math.Asin(2.0f * (q2.X * q2.Z - q2.W * q2.Y));
            outValue.Y = Math.Atan2(2.0f * q2.X * q2.W + 2.0f * q2.Y * q2.Z,
                1 - 2.0f * (q2.Z * q2.Z + q2.W * q2.W));
            outValue.Z = Math.Atan2(2.0f * q2.X * q2.Y + 2.0f * q2.Z * q2.W,
                1 - 2.0f * (q2.Y * q2.Y + q2.Z * q2.Z));

            return Normalize();
        }

        /// <summary>
        /// Creates a quaternion from a vector3 rotation with each member representing angles in degrees for that axis
        /// </summary>
        /// <param name="angles">The rotation per axis in degrees</param>
        /// <returns>The new quaternion</returns>
        public static Quaternion Euler(Vector3 angles)
        {
            return Quaternion.CreateFromYawPitchRoll(Math.Deg2Rad * angles.Y, Math.Deg2Rad * angles.X, Math.Deg2Rad * angles.Z);
        }

        /// <summary>
        /// Creates a quaternion from a vector3 rotation with each member representing angles in degrees for that axis
        /// </summary>
        /// <param name="x">The rotation in the x axis in degrees</param>
        /// <param name="y">The rotation in the y axis in degrees</param>
        /// <param name="z">The rotation in the z axis in degrees</param>
        /// <returns>The new quaternion</returns>
        public static Quaternion Euler(float x, float y, float z)
        {
            return Quaternion.CreateFromYawPitchRoll(Math.Deg2Rad * y, Math.Deg2Rad * x, Math.Deg2Rad * z);
        }

        /// <summary>
        /// Calculates a quaternion for a direction and up axis
        /// </summary>
        /// <param name="forward">The direction</param>
        /// <param name="up">The up axis</param>
        /// <returns>The rotation</returns>
        /// <remarks>From https://discussions.unity.com/t/what-is-the-source-code-of-quaternion-lookrotation/72474</remarks>
        public static Quaternion LookAt(Vector3 forward, Vector3 up)
        {
            var vector = forward.Normalized;
            var vector2 = up.Cross(vector);
            var vector3 = vector.Cross(vector2);

            var m00 = vector2.X;
            var m01 = vector2.Y;
            var m02 = vector2.Z;
            var m10 = vector3.X;
            var m11 = vector3.Y;
            var m12 = vector3.Z;
            var m20 = vector.X;
            var m21 = vector.Y;
            var m22 = vector.Z;

            float num8 = (m00 + m11) + m22;

            var quaternion = new Quaternion();

            if (num8 > 0f)
            {
                var num = (float)Math.Sqrt(num8 + 1f);

                quaternion.W = num * 0.5f;

                num = 0.5f / num;

                quaternion.X = (m12 - m21) * num;
                quaternion.Y = (m20 - m02) * num;
                quaternion.Z = (m01 - m10) * num;

                return quaternion;
            }
            if ((m00 >= m11) && (m00 >= m22))
            {
                var num7 = (float)Math.Sqrt(((1f + m00) - m11) - m22);
                var num4 = 0.5f / num7;

                quaternion.X = 0.5f * num7;
                quaternion.Y = (m01 + m10) * num4;
                quaternion.Z = (m02 + m20) * num4;
                quaternion.W = (m12 - m21) * num4;

                return quaternion;
            }

            if (m11 > m22)
            {
                var num6 = (float)Math.Sqrt(((1f + m11) - m00) - m22);
                var num3 = 0.5f / num6;

                quaternion.X = (m10 + m01) * num3;
                quaternion.Y = 0.5f * num6;
                quaternion.Z = (m21 + m12) * num3;
                quaternion.W = (m20 - m02) * num3;

                return quaternion;
            }

            var num5 = (float)Math.Sqrt(((1f + m22) - m00) - m11);
            var num2 = 0.5f / num5;

            quaternion.X = (m20 + m02) * num2;
            quaternion.Y = (m21 + m12) * num2;
            quaternion.Z = 0.5f * num5;
            quaternion.W = (m01 - m10) * num2;

            return quaternion;
        }

        /// <summary>
        /// Normalizes this quaternion and ensures the result is valid.
        /// A broken quaternion will typically normalize to an invalid value (<see cref="float.NaN"/> or <see cref="float.PositiveInfinity"/> for example).
        /// So we ensure we get a valid result by returning Identity if it's considered invalid.
        /// </summary>
        /// <returns>The normalized quaternion</returns>
        public Quaternion SafeNormalize()
        {
            var result = Quaternion.Normalize(q);

            if (float.IsNaN(result.X) || float.IsInfinity(result.X))
            {
                return Quaternion.Identity;
            }

            return result;
        }

        /// <summary>
        /// Rotates this quaternion
        /// </summary>
        /// <param name="angles">Angles to rotate by</param>
        /// <param name="relativeToWorld">Whether in world space or local space</param>
        public Quaternion Rotate(Vector3 angles, bool relativeToWorld = false)
        {
            var delta = Quaternion.CreateFromYawPitchRoll(angles.Y * Math.Deg2Rad, angles.X * Math.Deg2Rad, angles.Z * Math.Deg2Rad);

            return relativeToWorld ? (delta * q).Normalized : (q * delta).Normalized;
        }
    }
}
