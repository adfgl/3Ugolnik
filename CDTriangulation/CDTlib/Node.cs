using System.Globalization;

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

        public override string ToString()
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            return $"({Index}) {X.ToString(culture)}, {Y.ToString(culture)}";
        }
    }
}
