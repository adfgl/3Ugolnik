using System.Runtime.CompilerServices;

namespace CDTlib
{
    public class Node
    {
        public int Index { get; internal set; }
        public int Triangle { get; internal set; } = -1;
        public double X { get; internal set; }
        public double Y { get; internal set; }
        public double Z { get; internal set; }
        
        public Node()
        {
            
        }

        public Node(int index, double x, double y, double z)
        {
            Index = index;
            X = x;
            Y = y;
            Z = z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SquareDistance(Node a, Node b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return dx * dx + dy * dy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Cross(Node start, Node end, Node point)
        {
            double abx = end.X - start.X;
            double aby = end.Y - start.Y;
            double apx = point.X - start.X;
            double apy = point.Y - start.Y;
            return abx * apy - aby * apx;
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

            // Determinant
            double det = a * d - b * c;
            if (Math.Abs(det) < 1e-12)
                return null; // Lines are parallel or coincident

            double u = (e * d - b * f) / det;
            double v = (a * f - e * c) / det;

            if (u < 0 || u > 1 || v < 0 || v > 1)
                return null;

            double x = p1.X + u * a;
            double y = p1.Y + u * c;
            double z = p1.Z + u * (p2.Z - p1.Z); 

            return new Node(-1, x, y, z);
        }
    }
}
