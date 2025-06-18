namespace CDTlib
{
    public readonly struct Segment : IEquatable<Segment>
    {
        public readonly Node a, b;
        public readonly Circle circle;

        public Segment(Node a, Node b)
        {
            this.circle = new Circle(a.X, a.Y, b.X, b.Y);
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

        public void Split(Node node, out Segment a, out Segment b)
        {
            a = new Segment(this.a, node);
            b = new Segment(node, this.b);
        }

        public bool Equals(Segment other) => a.Index == other.a.Index && b.Index == other.b.Index;

        public override bool Equals(object? obj) => obj is Segment other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(a.Index, b.Index);

        public override string ToString()
        {
            return $"{a.Index} {b.Index}";
        }
    }
}
