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

        public override string ToString()
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            const int precision = 4;
            return $"({Index}) {Math.Round(X, precision).ToString(culture)}, {Math.Round(Y, precision).ToString(culture)}";
        }
    }
}
