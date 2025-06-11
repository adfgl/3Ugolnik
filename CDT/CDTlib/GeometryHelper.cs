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

        public static bool ConvexQuad(
            double x0, double y0,
            double x1, double y1,
            double x2, double y2,
            double x3, double y3)
        {
            return SameSide(x0, y0, x1, y1, x2, y2, x3, y3) &&
                   SameSide(x3, y3, x2, y2, x1, y1, x0, y0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool SameSide(
            double ax, double ay,
            double bx, double by,
            double cx, double cy,
            double dx, double dy)
        {
            double cross1 = Cross(ax, ay, bx, by, cx, cy);
            double cross2 = Cross(cx, cy, dx, dy, ax, ay);
            return cross1 * cross2 >= 0;
        }

        public static EOrientation ClassifyPoint(
            double ax, double ay,
            double bx, double by,
            double px, double py,
            double eps = 1e-12)
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

        public static bool Intersect(Vec2 p1, Vec2 p2, Vec2 q1, Vec2 q2, out Vec2 intersection)
        {
            // P(u) = p1 + u * (p2 - p1)
            // Q(v) = q1 + v * (q2 - q1)

            // goal to vind such 'u' and 'v' so:
            // p1 + u * (p2 - p1) = q1 + v * (q2 - q1)
            // which is:
            // u * (p2x - p1x) - v * (q2x - q1x) = q1x - p1x
            // u * (p2y - p1y) - v * (q2y - q1y) = q1y - p1y

            // | p2x - p1x  -(q2x - q1x) | *  | u | =  | q1x - p1x |
            // | p2y - p1y  -(q2y - q1y) |    | v |    | q1y - p1y |

            // | a  b | * | u | = | e |
            // | c  d |   | v |   | f |

            intersection = new Vec2();

            double a = p2.x - p1.x, b = q1.x - q2.x;
            double c = p2.y - p1.y, d = q1.y - q2.y;

            double det = a * d - b * c;
            if (Math.Abs(det) < 1e-12)
            {
                return false;
            }

            double e = q1.x - p1.x, f = q1.y - p1.y;

            double u = (e * d - b * f) / det;
            double v = (a * f - e * c) / det;
            if (u < 0 || u > 1 || v < 0 || v > 1)
            {
                return false;
            }
            intersection = new Vec2(p1.x + u * a, p1.y + u * c);
            return true;
        }
    }
}
