using System.Runtime.CompilerServices;

namespace CDTSharp
{
    public class Node : ICloneable, IEquatable<Node>
    {
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

        public int Index { get; set; } = -1;
        public int Triangle { get; set; } = -1;

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DistanceSquared(Node a, Node b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            double dz = a.Z - b.Z;
            return dx * dx + dy * dy + dz * dz;
        }

        public static double Distance(Node a, Node b)
        {
            return Math.Sqrt(DistanceSquared(a, b));
        }

        public override string ToString()
        {
            return $"[{Index}] {X} {Y} {Z}";
        }

        public object Clone()
        {
            return new Node(Index, X, Y, Z);
        }

        public bool Equals(Node? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            return X == other.X && Y == other.Y;
        }
    }
}
