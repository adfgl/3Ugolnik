using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharp
{
    public static class GeometryHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Cross(double startX, double startY, double endX, double endY, double x, double y)
        {
            return endX * (y - startY) - endY * (x - startX);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Cross(Node start, Node end, Node pt) => Cross(start.X, start.Y, end.X, end.Y, pt.X, pt.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Cross(HeNode start, HeNode end, HeNode pt) => Cross(start.X, start.Y, end.X, end.Y, pt.X, pt.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool QuadConvex(Node a, Node b, Node c, Node d)
        {
            double ab_bc = Cross(a, b, c);
            double bc_cd = Cross(b, c, d);
            double cd_da = Cross(c, d, a);
            double da_ab = Cross(d, a, b);
            return ab_bc > 0 && bc_cd > 0 && cd_da > 0 && da_ab > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool QuadConvex(HeNode a, HeNode b, HeNode c, HeNode d)
        {
            double ab_bc = Cross(a, b, c);
            double bc_cd = Cross(b, c, d);
            double cd_da = Cross(c, d, a);
            double da_ab = Cross(d, a, b);
            return ab_bc > 0 && bc_cd > 0 && cd_da > 0 && da_ab > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Angle(Node a, Node b, Node c)
        {
            double bax = a.X - b.X;
            double bay = a.Y - b.Y;
            double bcx = c.X - b.X;
            double bcy = c.Y - b.Y;

            double dot = bax * bcx + bay * bcy;
            double det = bax * bcy - bay * bcx;

            return Math.Atan2(Math.Abs(det), dot);
        }

        public static bool Intersect(double p1x, double p1y, double p2x, double p2y, double q1x, double q1y, double q2x, double q2y, out double x, out double y)
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

            x = y = Double.NaN;

            double a = p2x - p1x, b = q1x - q2x;
            double c = p2y - p1y, d = q1y - q2y;
            double e = q1x - p1x, f = q1y - p1y;

            double det = a * d - b * c;
            if (Math.Abs(det) < 1e-12)
            {
                return false;
            }

            double u = (e * d - b * f) / det;
            double v = (a * f - e * c) / det;

            if (u < 0 || u > 1 || v < 0 || v > 1)
            {
                return false;
            }

            x = p1x + u * a;
            y = p1y + u * c;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DistanceSquared(Node a, Node b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        public static double Distance(Node a, Node b)
        {
            return Math.Sqrt(DistanceSquared(a, b));
        }

        public static bool PointOnSegment(Node a, Node b, Node p, double tolerance = 1e-9)
        {
            if (p.X < Math.Min(a.X, b.X) - tolerance || p.X > Math.Max(a.X, b.X) + tolerance ||
                p.Y < Math.Min(a.Y, b.Y) - tolerance || p.Y > Math.Max(a.Y, b.Y) + tolerance)
                return false;

            double dx = b.X - a.X;
            double dy = b.Y - a.Y;

            double dxp = p.X - a.X;
            double dyp = p.Y - a.Y;

            double cross = dx * dyp - dy * dxp;
            if (Math.Abs(cross) > tolerance)
                return false;

            double dot = dx * dx + dy * dy;
            if (dot < tolerance)
                return Distance(a, p) <= tolerance;

            double t = (dxp * dx + dyp * dy) / dot;
            return t >= -tolerance && t <= 1 + tolerance;
        }

        public static bool Contains(List<Node> closedPolygon, double x, double y, double tolerance = 0)
        {
            int count = closedPolygon.Count - 1;
            bool inside = false;

            Node pt = new Node(-1, x, y);
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                Node a = closedPolygon[i];
                Node b = closedPolygon[j];
                if (PointOnSegment(a, b, pt, tolerance))
                {
                    return true;
                }

                double xi = a.X, yi = a.Y;
                double xj = b.X, yj = b.Y;

                bool crosses = (yi > y + tolerance) != (yj > y + tolerance);
                if (!crosses) continue;

                double t = (y - yi) / (yj - yi + double.Epsilon);
                double xCross = xi + t * (xj - xi);

                if (x < xCross - tolerance)
                    inside = !inside;
            }

            return inside;
        }

 
    }
}
