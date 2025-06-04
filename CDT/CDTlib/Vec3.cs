using System.Globalization;
using System.Runtime.CompilerServices;

namespace CDTlib
{
    public readonly struct Vec3
    {
        public readonly double x, y, z;

        public Vec3(double x, double y, double z)
        {
            this.x = x; this.y = y; this.z = z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dot(Vec3 a, Vec3 b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec3 Cross(Vec3 a, Vec3 b)
        {
            return new Vec3(
                a.y * b.z - a.z * b.y,
                a.z * b.x - a.x * b.z,
                a.x * b.y - a.y * b.x
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SquareLength(Vec3 v) => v.x * v.x + v.y * v.y + v.z * v.z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Length() => Math.Sqrt(x * x + y * y + z * z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec3 Normalize()
        {
            double length = Length();
            if (length == 0)
            {
                throw new DivideByZeroException();
            }
            return new Vec3(x / length, y / length, z / length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.x - b.x, a.y - b.y, a.z - b.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec3 operator -(Vec3 v) => new Vec3(-v.x, -v.y, -v.z);

        public override string ToString()
        {
            return $"{x.ToString(CultureInfo.InvariantCulture)}, {y.ToString(CultureInfo.InvariantCulture)}, {z.ToString(CultureInfo.InvariantCulture)}";
        }
    }
}
