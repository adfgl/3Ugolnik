using System.Runtime.CompilerServices;

namespace CDTlib
{
    public class Node
    {
        public int Index { get; set; }
        public int Triangle { get; set; } = -1;
        public double X { get; set; }
        public double Y { get; set; }
        
        public Node()
        {
            
        }

        public Node(int index, double x, double y)
        {
            Index = index;
            X = x;
            Y = y;
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
    }
}
