using System.Globalization;

namespace CDTlib.DataStructures
{
    public class Node
    {
        public Node(int index, double x, double y)
        {
            Index = index;
            X = x;
            Y = y;
        }

        public int Index { get; }
        public double X { get; set; }
        public double Y { get; set; }
        public Edge Edge { get; set; } = null!;

        public IEnumerable<Edge> Around()
        {
            Edge start = Edge;
            Edge current = start;
            while (true)
            {
                yield return current;
                current = current.Twin!.Next;
                if (current == start || current == null)
                    yield break;
            }
        }

        public override string ToString()
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            return $"({Index}) {X.ToString(culture)}, {Y.ToString(culture)}";
        }
    }
}
