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
