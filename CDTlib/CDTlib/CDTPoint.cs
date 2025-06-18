namespace CDTlib
{
    public class CDTPoint : IEquatable<CDTPoint>
    {
        public CDTPoint()
        {

        }

        public CDTPoint(double x, double y, double z = 0)
        {
            X = x; Y = y; Z = z;
        }

        public void Deconstruct(out double x, out double y)
        {
            x = X;
            y = Y;
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public bool Equals(CDTPoint? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            return X == other.X && Y == other.Y;
        }
    }
}
