using CDTlib.Utils;
using System.Runtime.CompilerServices;

namespace CDTlib
{
    public enum EOrientation
    {
        NodeA, NodeB, Edge, Left, Right
    }

    public static class GeometryHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Area(double x0, double y0, double x1, double y1, double x2, double y2)
        {
            return Cross(x0, y0, x1, y1, x2, y2) * 0.5;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Cross(double x, double y, double x1, double y1, double x2, double y2)
        {
            double abx = x1 - x, aby = y1 - y;
            double acx = x2 - x, acy = y2 - y;
            return abx * acy - aby * acx;
        }

        public static EOrientation ClassifyPoint(double ax, double ay, double bx, double by, double px, double py, double eps = 1e-12)
        {
            double dax = ax - px, day = ay - py;
            if (dax * dax + day * day < eps * eps)
            {
                return EOrientation.NodeA;
            }

            double dbx = bx - px, dby = by - py;
            if (dbx * dbx + dby * dby < eps * eps)
            {
                return EOrientation.NodeB;
            }

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
