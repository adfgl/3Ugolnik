namespace CDTlib
{
    public class CDTNode : IEquatable<CDTNode>
    {
        public CDTNode()
        {

        }

        public CDTNode(double x, double y, double z = 0)
        {
            X = x; Y = y; Z = z;
        }

        public int Index { get; set; } = -1;
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public bool Equals(CDTNode? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            return X == other.X && Y == other.Y;
        }
    }
}
