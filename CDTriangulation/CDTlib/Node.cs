using System.Globalization;
using System.Runtime.CompilerServices;

namespace CDTlib
{
    public class Node
    {
        public Node(int index, double x, double y)
        {
            Index = index;
            X = x;
            Y = y;
        }

        public void Deconstruct(out double x, out double y)
        {
            x = X; 
            y = Y;
        }

        public int Index { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public Edge Edge { get; set; } = null!;

        public IEnumerable<Edge> Forward()
        {
            Edge start = Edge;
            Edge? current = start;

            yield return current;

            while (true)
            {
                Edge? twin = current.Twin;
                if (twin == null)
                    yield break;

                current = twin.Next;
                if (current == start)
                    yield break;

                yield return current;
            }
        }

        public IEnumerable<Edge> Backward()
        {
            Edge start = Edge;
            Edge? current = start.Prev;

            while (true)
            {
                Edge? twin = current.Twin;
                if (twin == null)
                    yield break;

                current = twin.Prev;
                if (current == start)
                    yield break;

                yield return current;
            }
        }

        public IEnumerable<Edge> Around()
        {
            using IEnumerator<Edge> forward = Forward().GetEnumerator();
            if (!forward.MoveNext())
                yield break;

            yield return forward.Current;

            bool hitNull = false;
            while (forward.MoveNext())
            {
                if (forward.Current.Twin == null)
                {
                    hitNull = true;
                    break;
                }

                yield return forward.Current;
            }

            if (hitNull)
            {
                foreach (Edge e in Backward())
                    yield return e;
            }
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

            double det = a * d - b * c;
            if (Math.Abs(det) < 1e-12)
            {
                return null;
            }

            double e = q1.X - p1.X, f = q1.Y - p1.Y;

            double u = (e * d - b * f) / det;
            double v = (a * f - e * c) / det;
            if (u < 0 || u > 1 || v < 0 || v > 1)
            {
                return null;
            }
            return new Node(-1, p1.X + u * a, p1.Y + u * c);
        }

        public override string ToString()
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            const int precision = 4;
            return $"({Index}) {Math.Round(X, precision).ToString(culture)}, {Math.Round(Y, precision).ToString(culture)}";
        }
    }
}
