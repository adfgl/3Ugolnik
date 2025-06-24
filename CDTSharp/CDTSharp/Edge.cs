namespace CDTSharp
{
    public readonly struct Edge : IEquatable<Edge>
    {
        public readonly Node a, b;
        public readonly Circle circle;

        public Edge(Node a, Node b)
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

        public void Split(Node node, out Edge a, out Edge b)
        {
            a = new Edge(this.a, node);
            b = new Edge(node, this.b);
        }

        public bool Equals(Edge other)
        {
            return a.Index == other.a.Index && b.Index == other.b.Index;
        }

        public override bool Equals(object? obj)
        {
            return obj is Edge other && Equals(other);
        }

        public override int GetHashCode() => HashCode.Combine(a.Index, b.Index);

        public override string ToString()
        {
            return $"{a.Index} {b.Index}";
        }
    }
}
