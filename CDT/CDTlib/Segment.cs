using CDTlib.DataStructures;

namespace CDTlib
{
    public readonly struct Segment : IEquatable<Segment>
    {
        public readonly double cx, cy, lengthSqr;
        public readonly Node a, b;

        public Segment(Node a, Node b)
        {
            this.cx = (a.X + b.X) * 0.5;
            this.cy = (a.Y + b.Y) * 0.5;

            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            this.lengthSqr = dx * dx + dy * dy;

            if (a.Index < b.Index)
            {
                this.a = a;
                this.b = b;
            }
            else
            {
                this.a = b;
                this.b = a;
            }
        }

        public bool Equals(Segment other) => a.Index == other.a.Index && b.Index == other.b.Index;

        public override bool Equals(object? obj) => obj is Segment other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(a.Index, b.Index);

        public override string ToString()
        {
            return $"{a} {b}";
        }
    }
}
