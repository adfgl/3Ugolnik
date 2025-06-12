namespace CDTlib
{
    public class Edge
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

        public override string ToString()
        {
            return $"{Origin.Index} {Next.Origin.Index}";
        }
    }
}
