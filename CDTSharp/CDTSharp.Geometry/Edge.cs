namespace CDTSharp.Geometry
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

        public Node Origin { get; }
        public Edge Next { get; set; } = null!;
        public Edge Prev => Next.Next;
        public Edge? Twin { get; set; } = null;
        public Triangle Triangle { get; set; } = null!;
        public bool Constrained { get; set; } = false;

        public double SquareLength()
        {
            var (sx, sy) = Origin;
            var (ex, ey) = Next.Origin;
            double dx = ex - sx;
            double dy = ey - sy;
            return dx * dx + dy * dy;
        }

        public bool Contains(Node node)
        {
            return Origin == node || Next.Origin == node;
        }

        public double Orientation(double x, double y)
        {
            return GeometryHelper.Cross(Origin, Next.Origin, x, y);
        }

        public void CopyProperties(Edge? twin)
        {
            Twin = twin;
            if (twin is not null)
            {
                Constrained = twin.Constrained;
            }
        }

        public void SetTwin(Edge? twin)
        {
            Twin = twin;
            if (twin is not null)
            {
                twin.Twin = this;
            }
        }

        public void SetConstraint(bool value)
        {
            Constrained = value;
            if (Twin is not null)
            {
                Twin.Constrained = value;
            }
        }
    }
}
