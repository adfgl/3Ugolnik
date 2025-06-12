namespace CDTlib
{
    public class Edge : ISplittable
    {
        public Edge(Node origin)
        {
            Origin = origin;
        }

        public void Deconstruct(out Node start, out Node end)
        {
            start = Origin;
            end = Next.Origin;
        }

        public Node Origin { get; set; }
        public Edge Next { get; set; } = null!;
        public Edge Prev { get; set; } = null!;
        public Edge? Twin { get; set; } = null;
        public Face Face { get; set; } = null!;
        public bool Constrained { get; set; } = false;

        public void SetPrev(Edge value)
        {
            Prev = value;
            value.Next = this;
        }

        public void SetNext(Edge value)
        {
            Next = value;
            value.Prev = this;
        }

        public void SetTwin(Edge? twin)
        {
            Twin = twin;
            if (twin is not null)
            {
                twin.Twin = this;
            }
        }

        public void SetConstraint(bool value = true)
        {
            Constrained = value;
            if (Twin is not null)
            {
                Twin.Constrained = value;
            }
        }

        public SplitResult Split(Node node)
        {
            bool constrained = Constrained;
            if (Twin is null)
            {

            }

            return null;
        }


        void SplitNoTwin(Node node)
        {
            var (a, b, c) = Face;
            Edge ab = Face.Edge;
            Edge bc = ab.Next;
            Edge ca = bc.Next;

            a.Edge = ab;
            b.Edge = bc;
            c.Edge = ca;

            Face new0 = new Face(Face.Index, node, c, a);
            Face new1 = new Face(-1, node, b, c); 

            new0.Edge.SetTwin(new1.Edge);

            new0.Edge.Prev.Twin = new1.Edge.Next.Twin = null;
            new0.Edge.Prev.Constrained = new1.Edge.Next.Constrained = Constrained;
        }

        public override string ToString()
        {
            var (a, b) = this;
            return $"{a.Index} {b.Index}";
        }
    }
}
