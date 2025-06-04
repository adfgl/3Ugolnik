using System.Runtime.CompilerServices;

namespace CDTlib
{
    public readonly struct Plane
    {
        public readonly Vec3 normal;
        public readonly double distance;

        public Plane(Vec3 normal, double distance)
        {
            this.normal = normal;
            this.distance = distance;
        }

        public Plane(Vec3 a, Vec3 b, Vec3 c)
        {
            this.normal = Vec3.Cross(c - a, b - a).Normalize();
            this.distance = Vec3.Dot(normal, a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double SignedDistanceTo(Vec3 point)
        {
            return Vec3.Dot(normal, point) - distance;
        }

        public Plane Flip() => new Plane(-this.normal, -distance);
    }
}
