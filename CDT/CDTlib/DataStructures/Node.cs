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

        public int Index { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public Edge Edge { get; set; } = null!;

        public override string ToString()
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            return $"[{Index}] {X.ToString(culture)}, {Y.ToString(culture)}";
        }
    }
}
