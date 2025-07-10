using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace TriSharp
{
    using static Triangle;

    public class Vertex
    {
        public Vertex()
        {

        }

        public Vertex(int index, double x, double y, double z = 0)
        {
            Index = index;
            X = x;
            Y = y;
            Z = z;
        }

        public void Deconstruct(out double x, out double y)
        {
            x = X; y = Y;
        }

        public int Index { get; set; } = NO_INDEX;
        public int Triangle { get; set; } = NO_INDEX;

        public double X { get; set; }   
        public double Y { get; set; }
        public double Z { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool QuadConvex(Vertex a, Vertex b, Vertex c, Vertex d)
        {
            return 
                Cross(a, b, c) > 0 && 
                Cross(b, c, d) > 0 && 
                Cross(c, d, a) > 0 && 
                Cross(d, a, b) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CloseOrEqual(Vertex a, Vertex b, double eps)
        {
            if (a.Index == b.Index && a.Index != NO_INDEX)
            {
                return true;
            }
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return dx * dx + dy * dy <= eps;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Between(Vertex a, Vertex b, out double x, out double y)
        {
            x = (b.X + a.X) * 0.5;
            y = (b.Y + a.Y) * 0.5;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SquareDistance(Vertex a, Vertex b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return dx * dx + dy * dy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Cross(Vertex a, Vertex b, Vertex c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }

        public static Vertex? Intersect(Vertex p1, Vertex p2, Vertex q1, Vertex q2)
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

            double det = a * d - b * c;
            if (Math.Abs(det) < 1e-12)
            {
                return null; // Lines are parallel or too close to parallel
            }

            double e = q1.X - p1.X, f = q1.Y - p1.Y;
            double u = (e * d - b * f) / det;
            double v = (a * f - e * c) / det;

            if (u < 0 || u > 1 || v < 0 || v > 1)
            {
                return null;
            }

            return new Vertex
            {
                X = p1.X + u * a,
                Y = p1.Y + u * c,
                Z = p1.Z + u * (p2.Z - p1.Z),
            };
        }


        public override string ToString()
        {
            return $"{X} {Y}";
        }
    }
}
