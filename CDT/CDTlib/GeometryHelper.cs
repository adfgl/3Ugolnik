using CDTlib.Utils;
using System.Runtime.CompilerServices;

namespace CDTlib
{
    public enum EOrientation
    {
        Edge, Left, Right
    }

    public static class GeometryHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Area(double x0, double y0, double x1, double y1, double x2, double y2)
        {
            return Cross(x0, y0, x1, y1, x2, y2) * 0.5;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Cross(double ax, double ay, double bx, double by, double px, double py)
        {
            double abx = bx - ax, aby = by - ay;
            double apx = px - ax, apy = py - ay;
            return abx * apy - aby * apx;
        }

        public static EOrientation ClassifyPoint(double ax, double ay, double bx, double by, double px, double py, double eps = 1e-12)
        {
            double abx = bx - ax, aby = by - ay;
            double apx = px - ax, apy = py - ay;

            double cross = abx * apy - aby * apx;

            if (Math.Abs(cross) < eps)
            {
                return EOrientation.Edge;
            }

            if (cross > 0)
            {
                return EOrientation.Left;
            }
            return EOrientation.Right;
        }
    }
}
