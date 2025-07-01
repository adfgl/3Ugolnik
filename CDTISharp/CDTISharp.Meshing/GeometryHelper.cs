using System.Runtime.CompilerServices;
using static System.Net.Mime.MediaTypeNames;

namespace CDTISharp.Meshing
{
    public static class GeometryHelper
    {
        public static bool CloseOrEqual(Node a, Node b, double eps)
        {
            if (a == b) return true;
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return dx * dx + dy * dy < eps;
        }

        public static double SquareLength(Node start, Node end)
        {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            return dx * dx + dy * dy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Cross(Node start, Node end, double x, double y)
        {
            return end.X * (y - start.Y) - end.Y * (x - start.X);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool QuadConvex(Node a, Node b, Node c, Node d)
        {
            double ab_bc = Cross(a, b, c.X, c.Y);
            double bc_cd = Cross(b, c, d.X, d.Y);
            double cd_da = Cross(c, d, a.X, a.Y);
            double da_ab = Cross(d, a, b.X, b.Y);
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

        public static Node? Intersect(Node p1, Node p2, Node q1, Node q2)
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

            double a = p2.X - p1.X, b = q1.X - q2.X;
            double c = p2.Y - p1.Y, d = q1.Y - q2.Y;
            double e = q1.X - p1.X, f = q1.Y - p1.Y;

            double det = a * d - b * c;
            if (Math.Abs(det) < 1e-12)
            {
                return null; // Lines are parallel or too close to parallel
            }

            double u = (e * d - b * f) / det;
            double v = (a * f - e * c) / det;

            if (u < 0 || u > 1 || v < 0 || v > 1)
            {
                return null; // No intersection within segments
            }

            return new Node
            {
                X = p1.X + u * a,
                Y = p1.Y + u * c,
                Z = p1.Z + u * (p2.Z - p1.Z), // Interpolated Z from segment P
                Index = -1
            };
        }

        public static bool Contains(List<Node> closedPolygon, double x, double y, double tolerance)
        {
            int count = closedPolygon.Count - 1;
            bool inside = false;

            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                Node a = closedPolygon[i];
                Node b = closedPolygon[j];
                if (PointOnSegment(a, b, x, y, tolerance))
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

        public static bool PointOnSegment(Node a, Node b, double x, double y, double tolerance)
        {
            if (x < Math.Min(a.X, b.X) - tolerance || x > Math.Max(a.X, b.X) + tolerance ||
                y < Math.Min(a.Y, b.Y) - tolerance || y > Math.Max(a.Y, b.Y) + tolerance)
                return false;

            double dx = b.X - a.X;
            double dy = b.Y - a.Y;

            double dxp = x - a.X;
            double dyp = y - a.Y;

            double cross = dx * dyp - dy * dxp;
            if (Math.Abs(cross) > tolerance)
                return false;

            double dot = dx * dx + dy * dy;
            if (dot < tolerance)
            {
                double ddx = a.X - x;
                double ddy = a.Y - y;
                return ddx * ddx + ddy * ddy <= tolerance;
            }

            double t = (dxp * dx + dyp * dy) / dot;
            return t >= -tolerance && t <= 1 + tolerance;
        }
    }
}
